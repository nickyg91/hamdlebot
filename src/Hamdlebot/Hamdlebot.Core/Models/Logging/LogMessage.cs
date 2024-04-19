namespace Hamdlebot.Core.Models.Logging;

public record LogMessage(string Message, DateTime Timestamp, SeverityLevel SeverityLevel)
{
}