namespace Hamdlebot.Data.Contexts;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}