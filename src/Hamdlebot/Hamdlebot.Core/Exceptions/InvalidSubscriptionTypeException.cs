namespace Hamdlebot.Core.Exceptions;

public class InvalidSubscriptionTypeException : Exception
{
    public InvalidSubscriptionTypeException(string? message) : base(message)
    {
    }
}