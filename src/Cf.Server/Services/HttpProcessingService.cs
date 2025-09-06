using Cf.Server.Config;
using Cf.Server.Interfaces;
using Cf.Server.Models;

namespace Cf.Server.Services;

public class HttpProcessingService
{
    private readonly IBrokerService _brokerService;
    private readonly IRequestCollapser _requestCollapser;
    private readonly BrokerConfig _config;
    private readonly ILogger<HttpProcessingService> _logger;

    public HttpProcessingService(
        IBrokerService brokerService,
        IRequestCollapser requestCollapser,
        BrokerConfig config,
        ILogger<HttpProcessingService> logger)
    {
        _brokerService = brokerService;
        _requestCollapser = requestCollapser;
        _config = config;
        _logger = logger;
    }

    public async Task<BrokerResponse> ProcessRequestPrimitiveAsync(BrokerRequest request, CancellationToken cancellationToken = default)
    {
        return await _brokerService.SendRequestAsync(request, cancellationToken);
    }

    public async Task<BrokerResponse> ProcessRequestAdvancedAsync(BrokerRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _requestCollapser.ExecuteWithCollapsingAsync(
            request,
            async (req, ct) => await _brokerService.SendRequestAsync(req, ct),
            cancellationToken);
    }
}
