namespace Hamdlebot.Models.OBS.RequestTypes;

public class IdentifyRequest
{
    public int RpcVersion { get; set; }
    public string? Authentication { get; set; }
    public int? EventSubscriptions { get; set; }
}