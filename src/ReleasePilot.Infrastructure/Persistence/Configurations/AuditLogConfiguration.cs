using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EventType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.PromotionId).IsRequired();
        builder.Property(a => a.Timestamp).IsRequired();
        builder.Property(a => a.ActingUser).HasMaxLength(256);

        builder.HasIndex(a => a.PromotionId);
        builder.HasIndex(a => a.Timestamp);
    }
}
