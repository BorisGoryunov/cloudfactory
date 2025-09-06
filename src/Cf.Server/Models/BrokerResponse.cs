namespace Cf.Server.Models;

public class BrokerResponse
{
    public int StatusCode { get; set; }
    
    public string? Body { get; set; }
    
    public Dictionary<string, string> Headers { get; set; } = new();
}
