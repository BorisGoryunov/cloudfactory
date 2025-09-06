namespace Cf.Server.Config;

public class AppConfig
{
    public required string BrokerDirectory { get; init; }

    public bool UseAdvancedMode { get; init; }
}
