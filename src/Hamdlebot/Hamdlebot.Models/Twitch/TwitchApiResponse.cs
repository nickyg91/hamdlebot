using System.Text.Json.Serialization;

namespace Hamdlebot.Models.Twitch;

public class TwitchApiResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; }
}