using Cf.Server.Models;

namespace Cf.Server.Interfaces;

public interface IRequestCollapser
{
    Task<BrokerResponse> ExecuteWithCollapsingAsync(
        BrokerRequest request, 
        Func<BrokerRequest, CancellationToken, Task<BrokerResponse>> requestFunc,
        CancellationToken cancellationToken = default);    
}
