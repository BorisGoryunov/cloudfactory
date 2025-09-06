using Cf.Server.Models;

namespace Cf.Server.Interfaces;

public interface IBrokerService 
{
    Task<BrokerResponse> SendRequest(BrokerRequest request, CancellationToken cancellationToken = default);

    void Initialize();
}
