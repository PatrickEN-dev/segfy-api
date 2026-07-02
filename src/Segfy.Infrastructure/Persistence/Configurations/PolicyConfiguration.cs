using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.ValueObjects;

namespace Segfy.Infrastructure.Persistence.Configurations;

public sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasConversion<string>();

        builder.Property(p => p.Number)
            .HasConversion(v => v.Value, s => PolicyNumber.Parse(s))
            .HasColumnName("Number")
            .IsRequired();
        builder.HasIndex(p => p.Number).IsUnique();

        builder.Property(p => p.Document)
            .HasConversion(v => v.Digits, s => Document.LoadTrusted(s))
            .HasColumnName("Document")
            .IsRequired();

        builder.Property(p => p.LicensePlate)
            .HasConversion(v => v.Value, s => LicensePlate.LoadTrusted(s))
            .HasColumnName("LicensePlate")
            .IsRequired();

        // Defense-in-depth: SQLite partial unique index enforces "one Ativa policy per plate"
        // at the database level. The Application layer also checks it, but the DB guard
        // catches concurrent creates that could slip past the read-then-write TOCTOU window.
        builder.HasIndex(p => p.LicensePlate)
            .IsUnique()
            .HasFilter("Status = 'Ativa'")
            .HasDatabaseName("IX_Policies_LicensePlate_ActiveUnique");

        builder.Property(p => p.Premium)
            .HasConversion(
                v => v.Amount.ToString("F2", CultureInfo.InvariantCulture),
                s => Money.LoadTrusted(decimal.Parse(s, CultureInfo.InvariantCulture)))
            .HasColumnName("PremiumAmount")
            .IsRequired();

        builder.Property(p => p.CoverageStart)
            .HasConversion(
                v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                s => DateOnly.Parse(s, CultureInfo.InvariantCulture))
            .HasColumnName("CoverageStart")
            .IsRequired();

        builder.Property(p => p.CoverageEnd)
            .HasConversion(
                v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                s => DateOnly.Parse(s, CultureInfo.InvariantCulture))
            .HasColumnName("CoverageEnd")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            .IsRequired();

        builder.Ignore(p => p.Coverage);

        var history = builder.Metadata.FindNavigation(nameof(Policy.StatusHistory))!;
        history.SetPropertyAccessMode(PropertyAccessMode.Field);
        history.SetField("_statusHistory");

        builder.HasMany(p => p.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
