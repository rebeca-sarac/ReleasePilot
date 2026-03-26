using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Persistence.Configurations;

public class PromotionStateHistoryConfiguration : IEntityTypeConfiguration<PromotionStateHistory>
{
    public void Configure(EntityTypeBuilder<PromotionStateHistory> builder)
    {
        builder.ToTable("PromotionStateHistory");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.PromotionId).IsRequired();
        builder.Property(h => h.State).IsRequired();
        builder.Property(h => h.OccurredOn).IsRequired();
        builder.Property(h => h.Actor).HasMaxLength(256);

        builder.HasIndex(h => h.PromotionId);
    }
}
