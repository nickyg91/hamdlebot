using System.Text.RegularExpressions;
using Hamdlebot.Core.Models;

namespace Hamdlebot.Core.Extensions;

public static partial class TwitchIrcExtensions
{
    public static bool IsPingMessage(this string ircMessage)
    {
        return ircMessage.Contains("PING :tmi.twitch.tv");
    }
    
    public static TwitchMessage ToTwitchMessage(this string ircMessage)
    {
        var userHandle = UsernameRegex().Match(ircMessage);
        var message = MessageRegex().Match(ircMessage)?.Groups["Message"]?.Value?.Trim().ToLower();
        var tagMatches = TagRegex().Matches(ircMessage);
        
        var dict = new Dictionary<string, string>();
        foreach (Match match in tagMatches)
        {
            var item = match.Value;
            var splitItem = item.Split("=");
            dict.TryAdd(splitItem[0], splitItem[1]);
        }

        dict.TryGetValue(TwitchIrcConstants.UserId, out var userId);
        dict.TryGetValue(TwitchIrcConstants.IsMod, out var isMod);
        dict.TryGetValue(TwitchIrcConstants.FirstMessage, out var isFirstMessage);
        dict.TryGetValue(TwitchIrcConstants.Id, out var messageId);
        dict.TryGetValue(TwitchIrcConstants.DisplayName, out var displayName);
        dict.TryGetValue(TwitchIrcConstants.IsSubscriber, out var isSubscriber);
        
        int.TryParse(userId, out var parsedUserId);
        bool.TryParse(isSubscriber, out var parsedIsSubscriber);
        bool.TryParse(isMod, out var parsedIsMod);
        bool.TryParse(isFirstMessage, out var parsedIsFirstMessage);
        
        var twitchIrcMessage = new TwitchMessage
        {
            Message = message ?? string.Empty,
            User = userHandle.Groups[0].Value,
            DisplayName = displayName ?? string.Empty,
            IsMod = parsedIsMod,
            IsSubscriber = parsedIsSubscriber,
            UserId = parsedUserId,
            MessageId = string.IsNullOrEmpty(messageId) ? null : Guid.Parse(messageId),
            IsFirstMessage = parsedIsFirstMessage
        };
        
        return twitchIrcMessage;
    }

    [GeneratedRegex(@"([\w]+!\w+@\w+.tmi.twitch.tv)")]
    private static partial Regex UsernameRegex();
    [GeneratedRegex(@"(?<EntireMessage>(PRIVMSG)(?<User>\s.*:{1})(?<Message>.+))")]
    private static partial Regex MessageRegex();
    [GeneratedRegex(@"(?<Tags>((\w(-*))+=(\w|#|:|-)+))")]
    private static partial Regex TagRegex();
}