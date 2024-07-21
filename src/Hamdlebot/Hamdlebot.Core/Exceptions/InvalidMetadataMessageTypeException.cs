namespace Hamdlebot.Core.Exceptions;

public class InvalidMetadataMessageTypeException : Exception
{
    public InvalidMetadataMessageTypeException(string? message) : base(message)
    {
    }
}