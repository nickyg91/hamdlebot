namespace Hamdlebot.Core.Exceptions;

public class TokenGenerationException : Exception
{
    public TokenGenerationException(string? message) : base(message)
    {
    }
}