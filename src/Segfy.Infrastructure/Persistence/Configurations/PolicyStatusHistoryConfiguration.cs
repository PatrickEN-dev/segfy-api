using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Segfy.Domain.Policies;

namespace Segfy.Infrastructure.Persistence.Configurations;

public sealed class PolicyStatusHistoryConfiguration : IEntityTypeConfiguration<PolicyStatusHistory>
{
    public void Configure(EntityTypeBuilder<PolicyStatusHistory> builder)
    {
        builder.ToTable("PolicyStatusHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasConversion<string>();
        builder.Property(x => x.PolicyId).HasConversion<string>().IsRequired();

        builder.Property(x => x.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.Property(x => x.ChangedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            .IsRequired();

        builder.HasIndex(x => x.PolicyId);
    }
}
