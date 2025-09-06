namespace Cf.Server.Models;

public class BrokerRequest
{
    public string Method { get; set; } = string.Empty;
    
    public string Path { get; set; } = string.Empty;
    
    public string? Body { get; set; }
    
    public Dictionary<string, string> Headers { get; set; } = new();
}
