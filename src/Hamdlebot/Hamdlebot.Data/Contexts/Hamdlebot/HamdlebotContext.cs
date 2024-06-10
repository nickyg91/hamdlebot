using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hamdlebot.Data.Contexts.Hamdlebot;

public class HamdlebotContext : DbContext
{
    public DbSet<BotChannel> BotChannels { get; set; }
    public DbSet<BotChannelCommand> BotChannelCommands { get; set; }
    
    public HamdlebotContext(DbContextOptions options) : base(options)
    {
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     base.OnConfiguring(optionsBuilder);
    // }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HamdlebotContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}