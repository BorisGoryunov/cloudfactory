namespace Cf.Server.Config;

public class BrokerConfig
{
    public int RequestTimeoutMs { get; init; }
    
    public int PollingIntervalMs { get; init; }
}
