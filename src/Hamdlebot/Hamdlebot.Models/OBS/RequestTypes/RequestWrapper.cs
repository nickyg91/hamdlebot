namespace Hamdlebot.Models.OBS.RequestTypes;

public class RequestWrapper<T>
{
    public Guid? RequestId { get; set; }
    public string? RequestType { get; set; }
    public T? RequestData { get; set; }
    public int? RpcVersion { get; set; }
}