using Hamdlebot.Data.Contexts.Hamdlebot.Entities;

namespace Hamdlebot.Models.ViewModels;

public class ChannelCommand
{
    public ChannelCommand()
    {
        
    }

    public ChannelCommand(BotChannelCommand command)
    {
        Id = command.Id;
        BotChannelId = command.BotChannelId;
        Command = command.Command;
        Response = command.Response;
    }
    public int? Id { get; set; }
    public int BotChannelId { get; set; }
    public string Command { get; set; }
    public string Response { get; set; }
}