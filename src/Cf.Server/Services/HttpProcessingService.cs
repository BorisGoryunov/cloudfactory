using Cf.Server.Config;
using Cf.Server.Interfaces;
using Cf.Server.Models;

namespace Cf.Server.Services;

public class HttpProcessingService
{
    private readonly IBrokerService _brokerService;
    private readonly IRequestCollapser _requestCollapser;

    public HttpProcessingService(
        IBrokerService brokerService,
        IRequestCollapser requestCollapser)
    {
        _brokerService = brokerService;
        _requestCollapser = requestCollapser;
    }

    public async Task<BrokerResponse> ProcessRequestPrimitive(BrokerRequest request, CancellationToken cancellationToken = default)
    {
        return await _brokerService.SendRequest(request, cancellationToken);
    }

    public async Task<BrokerResponse> ProcessRequestAdvanced(BrokerRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _requestCollapser.ExecuteWithCollapsing(
            request,
            async (req, ct) => await _brokerService.SendRequest(req, ct),
            cancellationToken);
    }
}
