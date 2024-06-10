using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hamdlebot.Data.Contexts.Hamdlebot.EntityConfigurations;

public class BotChannelCommandEntityConfiguration : BaseEntityConfiguration<BotChannelCommand>
{
    public override string TableName => "bot_channel_command";
    
    public override void Configure(EntityTypeBuilder<BotChannelCommand> builder)
    {
        builder
            .Property(x => x.Command)
            .HasColumnName("command")
            .HasMaxLength(64)
            .IsRequired();
        
        builder
            .Property(x => x.Response)
            .HasColumnName("response")
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .HasOne(x => x.BotChannel)
            .WithMany(x => x.BotChannelCommands)
            .HasForeignKey(x => x.BotChannelId)
            .HasAnnotation("fk_bot_channel_command_bot_channel_id", "true");
        
        base.Configure(builder);
    }
}