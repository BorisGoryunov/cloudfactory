namespace Cf.Server.Models;

public class RequestContext
{
    public string Key { get; set; } = string.Empty;
    
    public BrokerRequest Request { get; set; } = new();
    
    public TaskCompletionSource<BrokerResponse> CompletionSource { get; set; } = new();
    
    public CancellationTokenRegistration? CancellationRegistration { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
