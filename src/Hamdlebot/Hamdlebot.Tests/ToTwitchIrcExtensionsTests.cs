using FluentAssertions;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models;

namespace Hamdlebot.Tests;

public class ToTwitchIrcExtensionsTests
{
    [Fact(DisplayName = "Should parse IRC message")]
    public void ShouldParseIrcMessageCorrectly()
    {
        var expected = new TwitchMessage
        {
            DisplayName = "lovingt3s",
            UserId = 713936733,
            Message = "bleedPurple",
            User = "lovingt3s!lovingt3s@lovingt3s.tmi.twitch.tv",
            IsMod = false,
            IsSubscriber = false,
            MessageId = Guid.Parse("885196de-cb67-427a-baa8-82f9b0fcd05f")
        };
        
        var msg =
            "@badge-info=;badges=broadcaster/1;client-nonce=459e3142897c7a22b7d275178f2259e0;color=#0000FF;display-name=lovingt3s;emote-only=1;emotes=62835:0-10;first-msg=0;flags=;id=885196de-cb67-427a-baa8-82f9b0fcd05f;mod=0;room-id=713936733;subscriber=0;tmi-sent-ts=1643904084794;turbo=0;user-id=713936733;user-type= :lovingt3s!lovingt3s@lovingt3s.tmi.twitch.tv PRIVMSG #lovingt3s :bleedPurple";
        
        var twitchMessage = msg.ToTwitchMessage();
        twitchMessage.Should().Be(expected);
    }
}