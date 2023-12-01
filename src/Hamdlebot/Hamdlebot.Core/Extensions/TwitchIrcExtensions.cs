using System.Text.RegularExpressions;
using Hamdlebot.Core.Models;

namespace Hamdlebot.Core.Extensions;

public static class TwitchIrcExtensions
{
    public static TwitchMessage ToTwitchMessage(this string ircMessage)
    {
        var identifierRegex = @"([\w]+!\w+@\w+.tmi.twitch.tv)";
        var tagsRegex = @"(?<Tags>((\w(-*))+=(\w|#|:|-)+))";
        var messageRegex = @"(?<EntireMessage>(PRIVMSG)(?<User>\s.*:{1})(?<Message>.+))";
        var userHandle = Regex.Match(ircMessage, identifierRegex);
        var message = Regex.Match(ircMessage, messageRegex)?.Groups["Message"]?.Value?.Trim();

        var tagMatches = Regex.Matches(ircMessage, tagsRegex);
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
}