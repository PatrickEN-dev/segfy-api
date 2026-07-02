using Microsoft.EntityFrameworkCore;
using Segfy.Application.Abstractions;

namespace Segfy.Infrastructure.Persistence.Sequences;

public sealed class SqlitePolicyNumberSequence : IPolicyNumberSequence
{
    private readonly SegfyDbContext _db;

    public SqlitePolicyNumberSequence(SegfyDbContext db)
    {
        _db = db;
    }

    public async Task<int> NextForYearAsync(int year, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var row = await _db.PolicyNumberSequences
            .FirstOrDefaultAsync(x => x.Year == year, ct);

        if (row is null)
        {
            row = new PolicyNumberSequenceRow { Year = year, LastValue = 1 };
            _db.PolicyNumberSequences.Add(row);
        }
        else
        {
            row.LastValue += 1;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return row.LastValue;
    }
}
