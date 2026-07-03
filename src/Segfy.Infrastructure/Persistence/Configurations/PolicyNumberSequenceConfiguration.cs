using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Segfy.Infrastructure.Persistence.Configurations;

public sealed class PolicyNumberSequenceConfiguration : IEntityTypeConfiguration<PolicyNumberSequenceRow>
{
    public void Configure(EntityTypeBuilder<PolicyNumberSequenceRow> builder)
    {
        builder.ToTable("PolicyNumberSequences");
        builder.HasKey(x => x.Year);
        // Year is business data (2026), never a database-generated value. Without
        // this, EF marks INTEGER PKs as autoincrement on SQLite.
        builder.Property(x => x.Year).ValueGeneratedNever();
        builder.Property(x => x.LastValue).HasDefaultValue(0).IsRequired();
    }
}
