using Cf.Server.Models;

namespace Cf.Server.Interfaces;

public interface IBrokerService : IAsyncDisposable
{
    Task<BrokerResponse> SendRequestAsync(BrokerRequest request, CancellationToken cancellationToken = default);

    void Initialize();
}
