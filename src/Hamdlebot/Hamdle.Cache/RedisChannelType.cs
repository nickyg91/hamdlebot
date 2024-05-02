namespace Hamdle.Cache;

public static class RedisChannelType
{
    public static string OnSceneReceived => "scene:received";
    public static string StartHamdleScene => "scene:startHamdle";
    public static string BotTwitchToken => "bot:twitch:token";
    public static string ObsSettingsChanged => "obs:settings:changed";
}