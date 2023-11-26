using System.Text.Json.Serialization;
using Hamdlebot.Models.OBS.RequestTypes;

namespace Hamdlebot.Models.OBS;

public class ObsRequest<T> where T : class 
{
    [JsonPropertyName("d")]
    public RequestWrapper<T>? RequestData { get; set; }
    public OpCodeType? Op { get; set; }
    public string? RequestId { get; set; }
}