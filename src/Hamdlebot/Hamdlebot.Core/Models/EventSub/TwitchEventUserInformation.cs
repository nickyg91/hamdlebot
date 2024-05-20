using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub;

public class TwitchEventUserInformation
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    [JsonPropertyName("user_log")]
    public string UserLogin { get; set; }
    [JsonPropertyName("broadcaster_user_id")]
    public long BroadcasterUserId { get; set; }
    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; }
    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; }
    [JsonPropertyName("followed_at")]
    public DateTime FollowedAt { get; set; }
}