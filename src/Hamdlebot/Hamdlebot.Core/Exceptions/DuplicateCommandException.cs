namespace Hamdlebot.Core.Exceptions;

public class DuplicateCommandException : Exception
{
    public DuplicateCommandException(string? message) : base(message)
    {
    }
}