using Cf.Server.Models;

namespace Cf.Server.Services;

public class CollapsedRequest : IDisposable
{
    private readonly Func<Task<BrokerResponse>> _requestFunc;
    private readonly Task<BrokerResponse> _responseTask;
    private readonly List<TaskCompletionSource<BrokerResponse>> _waiters = [];
    private bool _completed;

    public CollapsedRequest(Func<Task<BrokerResponse>> requestFunc)
    {
        _requestFunc = requestFunc;
        _responseTask = ExecuteRequest();
    }

    public async Task<BrokerResponse> GetResponse(CancellationToken cancellationToken)
    {
        if (_completed)
        {
            return await _responseTask;
        }

        var tcs = new TaskCompletionSource<BrokerResponse>();
        await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        lock (_waiters)
        {
            _waiters.Add(tcs);
        }

        return await tcs.Task;
    }

    private async Task<BrokerResponse> ExecuteRequest()
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

    public void Dispose()
    {
        if (!_completed)
        {
            CompleteAllWaiters(null, new OperationCanceledException("Collapsed request disposed"));
        }
    }
}
