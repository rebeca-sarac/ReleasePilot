using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        // EF Core resolves the private Promotion(Guid id, ...) constructor
        // by matching parameter names (case-insensitive) to property names.
        // UsePropertyAccessMode(PreferFieldDuringConstruction) ensures that
        // fields are used when constructing entities and properties for reads.
        builder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

        // ---- Value objects mapped as owned entities ----
        // Private constructors are resolved by EF Core via parameter-name
        // convention:  private ApplicationId(Guid value) → Value property.

        builder.OwnsOne(p => p.ApplicationId, b =>
        {
            b.Property(a => a.Value).HasColumnName("ApplicationId")
                                    .IsRequired();
        });

        builder.OwnsOne(p => p.Version, b =>
        {
            b.Property(v => v.Value).HasColumnName("Version")
                                    .HasMaxLength(50)
                                    .IsRequired();
        });

        builder.OwnsOne(p => p.TargetEnvironment, b =>
        {
            b.Property(e => e.Value).HasColumnName("TargetEnvironment")
                                    .HasMaxLength(50)
                                    .IsRequired();
        });

        builder.OwnsOne(p => p.SourceEnvironment, b =>
        {
            b.Property(e => e.Value).HasColumnName("source_environment")
                                    .HasMaxLength(50);
        });

        builder.Property(p => p.State).IsRequired();
        builder.Property(p => p.RequestedBy).HasMaxLength(256).IsRequired();
        builder.Property(p => p.RequestedAt).IsRequired();
        builder.Property(p => p.ApprovedBy).HasMaxLength(256);
        builder.Property(p => p.ApprovedAt);
        builder.Property(p => p.StartedAt);
        builder.Property(p => p.CompletedAt);
        builder.Property(p => p.RollbackReason).HasMaxLength(2000);
        builder.Property(p => p.CancellationReason).HasMaxLength(2000);

        builder.PrimitiveCollection(p => p.IssueReferences)
               .HasColumnName("IssueReferences")
               .HasColumnType("text[]");

        builder.Ignore(p => p.DomainEvents);
    }
}
