using System.Text.Json.Serialization;

namespace Hamdlebot.Models.OBS.ResponseTypes;

public class ResponseWrapper<T>
{
    public Guid RequestId { get; set; }
    public string RequestType { get; set; }
    public T ResponseData { get; set; }
    public T Authentication { get; set; }
    public RequestStatus RequestStatus { get; set; }
}