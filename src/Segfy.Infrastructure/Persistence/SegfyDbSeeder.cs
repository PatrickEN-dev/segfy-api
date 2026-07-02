using Microsoft.EntityFrameworkCore;
using Segfy.Application.Abstractions;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.ValueObjects;

namespace Segfy.Infrastructure.Persistence;

public static class SegfyDbSeeder
{
    public static async Task SeedDevAsync(
        SegfyDbContext db,
        IClock clock,
        IPolicyNumberSequence sequence,
        CancellationToken ct)
    {
        if (await db.Policies.AnyAsync(ct))
            return;

        var today = clock.TodayUtc;
        var year = clock.UtcNow.Year;

        var seedRecipe = new (string Document, string Plate, decimal Premium,
            DateOnly Start, DateOnly End, PolicyStatus Status)[]
        {
            ("52998224725", "ABC1234", 199.90m,
                today.AddMonths(-2), today.AddDays(5), PolicyStatus.Ativa),
            ("39053344705", "DEF2G34", 249.50m,
                today.AddMonths(-1), today.AddDays(25), PolicyStatus.Ativa),
            ("11144477735", "GHI5678", 320.00m,
                today.AddDays(-30), today.AddDays(40), PolicyStatus.Ativa),
            ("11222333000181", "JKL9012", 500.00m,
                today.AddMonths(-6), today.AddDays(20), PolicyStatus.Cancelada),
            ("06990590000123", "MNO3456", 150.00m,
                today.AddYears(-2), today.AddYears(-1), PolicyStatus.Expirada),
            ("52998224725", "PQR7A89", 400.00m,
                today.AddMonths(-3), today.AddDays(90), PolicyStatus.Ativa),
        };

        foreach (var item in seedRecipe)
        {
            var sequential = await sequence.NextForYearAsync(year, ct);
            var policy = Policy.Create(
                PolicyNumber.Create(year, sequential),
                Document.Create(item.Document),
                LicensePlate.Create(item.Plate),
                Money.Create(item.Premium),
                CoveragePeriod.Create(item.Start, item.End),
                clock.UtcNow);

            if (item.Status != PolicyStatus.Ativa)
                policy.ChangeStatus(item.Status, clock.UtcNow);

            db.Policies.Add(policy);
        }

        await db.SaveChangesAsync(ct);
    }
}
