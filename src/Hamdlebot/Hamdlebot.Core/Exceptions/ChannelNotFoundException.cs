namespace Hamdlebot.Core.Exceptions;

public class ChannelNotFoundException : Exception
{
    public ChannelNotFoundException(string? message) : base(message)
    {
    }
}