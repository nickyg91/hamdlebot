namespace Hamdlebot.Core;

public class AppConfigSettings
{
    public TwitchConnectionInfo TwitchConnectionInfo { get; set; }
    public RedisSettings RedisSettingsOptions { get; set; }
    public ObsSettings ObsSettingsOptions { get; set; }
}