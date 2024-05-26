namespace Hamdlebot.Core.Exceptions;

public class SubscriptionEventNotSupportedException : Exception
{
    public SubscriptionEventNotSupportedException(string? message) : base(message)
    {
    }
}