using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Infrastructure.Persistence.Repositories;

public sealed class PolicyRepository : IPolicyRepository
{
    private const string ActivePlateConflictMarker = "Policies.LicensePlate";
    private const int SqliteConstraintErrorCode = 19;

    private readonly SegfyDbContext _db;

    public PolicyRepository(SegfyDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(Policy policy, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _db.Policies.Add(policy);
        return SaveWithConstraintTranslationAsync(policy, ct);
    }

    public Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct) =>
        _db.Policies.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Policy> Items, int Total)> ListAsync(
        PolicyListQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (whereClause, filterParams) = BuildWhereClause(query);
        var orderBy = $"{OrderColumn(query.SortBy)} {(query.SortDir == SortDirection.Asc ? "ASC" : "DESC")}";

#pragma warning disable EF1002 // SQL string is server-controlled; only positional params carry user input.
        var total = await _db.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM Policies{whereClause}", filterParams.ToArray())
            .SingleAsync(ct);
#pragma warning restore EF1002

        var pageParams = filterParams.Concat(new object[] { query.PageSize, (query.Page - 1) * query.PageSize }).ToArray();
        var listSql = $"SELECT * FROM Policies{whereClause} ORDER BY {orderBy} LIMIT {{{filterParams.Count}}} OFFSET {{{filterParams.Count + 1}}}";

        var items = await _db.Policies
            .FromSqlRaw(listSql, pageParams)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }

    private static (string WhereClause, List<object> Params) BuildWhereClause(PolicyListQuery query)
    {
        var conditions = new List<string>();
        var parameters = new List<object>();

        // Registers the value as a positional parameter and returns its "{n}" placeholder.
        string Param(object value)
        {
            parameters.Add(value);
            return "{" + (parameters.Count - 1).ToString(CultureInfo.InvariantCulture) + "}";
        }

        static string Contains(string term) => $"%{EscapeLikePattern(term)}%";

        if (query.Status is { } status)
            conditions.Add($"Status = {Param(status.ToString())}");
        if (!string.IsNullOrWhiteSpace(query.DocumentContains))
            conditions.Add($"Document LIKE {Param(Contains(query.DocumentContains.Trim()))} ESCAPE '\\'");
        if (!string.IsNullOrWhiteSpace(query.LicensePlateContains))
            conditions.Add($"LicensePlate LIKE {Param(Contains(query.LicensePlateContains.Trim().ToUpperInvariant()))} ESCAPE '\\'");
        if (!string.IsNullOrWhiteSpace(query.NumberContains))
            conditions.Add($"Number LIKE {Param(Contains(query.NumberContains.Trim().ToUpperInvariant()))} ESCAPE '\\'");

        var whereClause = conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
        return (whereClause, parameters);
    }

    private static string EscapeLikePattern(string input) =>
        input.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("%", "\\%", StringComparison.Ordinal)
             .Replace("_", "\\_", StringComparison.Ordinal);

    private static string OrderColumn(PolicySortField sortBy) => sortBy switch
    {
        PolicySortField.CoverageEnd => "CoverageEnd",
        PolicySortField.Premium => "PremiumAmount",
        _ => "CreatedAt",
    };

    public Task UpdateAsync(Policy policy, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(policy);

        foreach (var history in policy.StatusHistory)
        {
            if (_db.Entry(history).State == EntityState.Detached)
                _db.PolicyStatusHistory.Add(history);
        }

        return SaveWithConstraintTranslationAsync(policy, ct);
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct)
    {
        // Single round trip; status history rows go along via ON DELETE CASCADE.
        var rows = await _db.Policies.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public async Task<IReadOnlyList<Policy>> ListExpiringAsync(
        DateOnly today, int daysWindow, CancellationToken ct)
    {
        var todayStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var cutoffStr = today.AddDays(daysWindow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return await _db.Policies
            .FromSqlRaw(
                @"SELECT * FROM Policies
                  WHERE Status = {0}
                    AND CoverageEnd >= {1}
                    AND CoverageEnd <= {2}
                  ORDER BY CoverageEnd ASC",
                nameof(PolicyStatus.Ativa), todayStr, cutoffStr)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsActiveByPlateAsync(
        string plateValue, Guid? excludePolicyId, CancellationToken ct)
    {
        var sql = "SELECT COUNT(*) AS Value FROM Policies WHERE Status = {0} AND LicensePlate = {1}";
        var parameters = new List<object> { nameof(PolicyStatus.Ativa), plateValue };
        if (excludePolicyId is { } id)
        {
            sql += " AND Id <> {2}";
            parameters.Add(id.ToString());
        }
#pragma warning disable EF1002 // SQL string is server-controlled; only positional params carry user input.
        var count = await _db.Database.SqlQueryRaw<int>(sql, parameters.ToArray()).SingleAsync(ct);
#pragma warning restore EF1002
        return count > 0;
    }

    public async Task<IReadOnlyList<Policy>> ListActiveExpiredAsync(DateOnly today, CancellationToken ct)
    {
        var todayStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return await _db.Policies
            .FromSqlRaw(
                @"SELECT * FROM Policies
                  WHERE Status = {0}
                    AND CoverageEnd < {1}
                  ORDER BY CoverageEnd ASC",
                nameof(PolicyStatus.Ativa), todayStr)
            .AsTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PolicyStatusHistory>> ListStatusHistoryAsync(
        Guid policyId, CancellationToken ct) =>
        await _db.PolicyStatusHistory
            .AsNoTracking()
            .Where(h => h.PolicyId == policyId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(ct);

    public async Task SaveExpirationBatchAsync(IReadOnlyCollection<Policy> policies, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(policies);
        if (policies.Count == 0) return;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var policy in policies)
        foreach (var history in policy.StatusHistory)
        {
            if (_db.Entry(history).State == EntityState.Detached)
                _db.PolicyStatusHistory.Add(history);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task SaveWithConstraintTranslationAsync(Policy policy, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsActivePlateConflict(ex))
        {
            _db.ChangeTracker.Clear();
            throw new DomainValidationException(
                $"There is already an active policy for vehicle {policy.LicensePlate.Value}.");
        }
    }

    private static bool IsActivePlateConflict(DbUpdateException ex) =>
        ex.InnerException is SqliteException sqlite
        && sqlite.SqliteErrorCode == SqliteConstraintErrorCode
        && sqlite.Message.Contains(ActivePlateConflictMarker, StringComparison.Ordinal);
}
