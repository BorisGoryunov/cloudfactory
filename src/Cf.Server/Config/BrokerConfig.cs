namespace Cf.Server.Config;

public class BrokerConfig
{
    public int RequestTimeoutMs { get; init; } = 60000;
    public int PollingIntervalMs { get; init; } = 100;
    
    public int FileLockTimeoutMs { get; init; } = 5000;    
}
