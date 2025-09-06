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

    public async Task<BrokerResponse> ExecuteWithCollapsing(
        BrokerRequest request,
        Func<BrokerRequest, CancellationToken, Task<BrokerResponse>> requestFunc,
        CancellationToken cancellationToken = default)
    {
        var collapseKey = $"{request.Method}:{request.Path}";

        while (true)
        {
            var collapsedRequest = _collapsedRequests.GetOrAdd(collapseKey, 
                new CollapsedRequest(async () => await requestFunc(request, cancellationToken)));
            
            try
            {
                return await collapsedRequest.GetResponse(cancellationToken);
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
}
