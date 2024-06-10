using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hamdlebot.Data.Contexts.Hamdlebot.EntityConfigurations;

public class BotChannelEntityConfiguration : BaseEntityConfiguration<BotChannel>
{
    public override string TableName => "bot_channel";
    public override void Configure(EntityTypeBuilder<BotChannel> builder)
    {
        builder
            .Property(x => x.ChannelId)
            .HasColumnName("channel_id")
            .IsRequired();
        builder
            .Property(x => x.IsHamdleEnabled)
            .HasColumnName("is_hamdle_enabled")
            .IsRequired();

        builder
            .Property(x => x.TwitchChannelName)
            .HasColumnName("twitch_channel_name")
            .IsRequired();
        
        builder
            .HasIndex(x => x.ChannelId)
            .IsUnique()
            .HasAnnotation("idx_channel_id_unique", "true");
        
        base.Configure(builder);
    }
}