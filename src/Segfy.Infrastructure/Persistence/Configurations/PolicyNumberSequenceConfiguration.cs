using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Segfy.Infrastructure.Persistence.Configurations;

public sealed class PolicyNumberSequenceConfiguration : IEntityTypeConfiguration<PolicyNumberSequenceRow>
{
    public void Configure(EntityTypeBuilder<PolicyNumberSequenceRow> builder)
    {
        builder.ToTable("PolicyNumberSequences");
        builder.HasKey(x => x.Year);
        builder.Property(x => x.LastValue).HasDefaultValue(0).IsRequired();
    }
}
