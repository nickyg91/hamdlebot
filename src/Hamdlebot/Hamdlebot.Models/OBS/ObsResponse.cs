using System.Text.Json.Serialization;
using Hamdlebot.Models.OBS.ResponseTypes;

namespace Hamdlebot.Models.OBS;

public class ObsResponse<T> where T : class
{
    [JsonPropertyName("op")]
    public OpCodeType OpCode { get; set; }
    [JsonPropertyName("d")]
    public ResponseWrapper<T> Response { get; set; }
}