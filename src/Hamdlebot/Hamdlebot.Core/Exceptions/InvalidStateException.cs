namespace Hamdlebot.Core.Exceptions;

public class InvalidStateException : Exception
{
    public InvalidStateException(string? message) : base(message)
    {
    }
}