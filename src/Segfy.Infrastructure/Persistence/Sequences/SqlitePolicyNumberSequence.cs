using System.Globalization;
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
        // Single atomic UPSERT: inserts the year row on first use, increments it on
        // every later call, and returns the new value in the same statement. SQLite
        // serializes writers, so two concurrent requests can never get the same number.
        //
        // Runs through raw ADO because EF's SqlQueryRaw wraps the SQL in a subquery
        // (SELECT ... FROM (<sql>)), which is illegal around INSERT ... RETURNING.
        var connection = _db.Database.GetDbConnection();
        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                @"INSERT INTO PolicyNumberSequences (Year, LastValue) VALUES ($year, 1)
                  ON CONFLICT(Year) DO UPDATE SET LastValue = LastValue + 1
                  RETURNING LastValue";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "$year";
            parameter.Value = year;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            // Reference-counted by EF: only really closes if this call opened it.
            await _db.Database.CloseConnectionAsync();
        }
    }
}
