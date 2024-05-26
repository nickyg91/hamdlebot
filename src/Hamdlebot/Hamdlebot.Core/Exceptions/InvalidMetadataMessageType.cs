namespace Hamdlebot.Core.Exceptions;

public class InvalidMetadataMessageType : Exception
{
    public InvalidMetadataMessageType(string? message) : base(message)
    {
    }
}