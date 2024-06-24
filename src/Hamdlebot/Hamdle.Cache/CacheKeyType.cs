namespace Hamdle.Cache;

public static class CacheKeyType
{
    public static string TwitchOauthToken => "twitchOauthToken";
    public static string TwitchRefreshToken => "twitchRefreshToken";
    public static string TwitchAccessToken => "twitchAccessToken";
    public static string BotCommands => "commands";
    public static string IsStreamOnline => "isStreamOnline";
    public static string UserObsSettings => "userObsSettings";
}