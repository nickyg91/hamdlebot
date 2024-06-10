using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Hamdlebot.Data.Contexts;

public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual string TableName { get; }
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(x => x.Id).HasName($"pk_{TableName}");
        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityColumn();
        
        builder
            .Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");
    }
}