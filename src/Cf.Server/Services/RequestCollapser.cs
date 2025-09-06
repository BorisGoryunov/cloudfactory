using System.Collections.Concurrent;
using Cf.Server.Interfaces;
using Cf.Server.Models;

namespace Cf.Server.Services;

public class RequestCollapser : IRequestCollapser
{
    private readonly ConcurrentDictionary<string, CollapsedRequest> _collapsedRequests = new();
    private readonly ILogger<RequestCollapser> _logger;

    public RequestCollapser(ILogger<RequestCollapser> logger)
    {
        _logger = logger;
    }

    public async Task<BrokerResponse> ExecuteWithCollapsingAsync(
        BrokerRequest request,
        Func<BrokerRequest, CancellationToken, Task<BrokerResponse>> requestFunc,
        CancellationToken cancellationToken = default)
    {
        var collapseKey = $"{request.Method}:{request.Path}";

        while (true)
        {
            var collapsedRequest = _collapsedRequests.GetOrAdd(collapseKey, key =>
                new CollapsedRequest(key, async () => await requestFunc(request, cancellationToken)));

            try
            {
                return await collapsedRequest.GetResponseAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in collapsed request for key {Key}", collapseKey);
                
                if (_collapsedRequests.TryRemove(collapseKey, out var removed) 
                    && removed == collapsedRequest)
                {
                    continue;
                }
                
                throw;
            }
        }
    }

    private class CollapsedRequest : IAsyncDisposable
    {
        private readonly string _key;
        private readonly Func<Task<BrokerResponse>> _requestFunc;
        private readonly Task<BrokerResponse> _responseTask;
        private readonly List<TaskCompletionSource<BrokerResponse>> _waiters = new();
        private bool _completed;

        public CollapsedRequest(string key, Func<Task<BrokerResponse>> requestFunc)
        {
            _key = key;
            _requestFunc = requestFunc;
            _responseTask = ExecuteRequestAsync();
        }

        public async Task<BrokerResponse> GetResponseAsync(CancellationToken cancellationToken)
        {
            if (_completed)
            {
                return await _responseTask;
            }

            var tcs = new TaskCompletionSource<BrokerResponse>();
            var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            lock (_waiters)
            {
                _waiters.Add(tcs);
            }

            try
            {
                return await tcs.Task;
            }
            finally
            {
                await registration.DisposeAsync();
            }
        }

        private async Task<BrokerResponse> ExecuteRequestAsync()
        {
            try
            {
                var response = await _requestFunc();
                CompleteAllWaiters(response, null);
                return response;
            }
            catch (Exception ex)
            {
                CompleteAllWaiters(null, ex);
                throw;
            }
            finally
            {
                _completed = true;
            }
        }

        private void CompleteAllWaiters(BrokerResponse? response, Exception? exception)
        {
            List<TaskCompletionSource<BrokerResponse>> waiters;
            lock (_waiters)
            {
                waiters = new List<TaskCompletionSource<BrokerResponse>>(_waiters);
                _waiters.Clear();
            }

            foreach (var waiter in waiters)
            {
                if (exception != null)
                {
                    waiter.TrySetException(exception);
                }
                else
                {
                    waiter.TrySetResult(response!);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                CompleteAllWaiters(null, new OperationCanceledException("Collapsed request disposed"));
            }
        }
    }
}
