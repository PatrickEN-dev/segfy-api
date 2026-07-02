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

        void AddLike(string columnSql, string rawValue)
        {
            conditions.Add(string.Format(CultureInfo.InvariantCulture, columnSql, parameters.Count));
            parameters.Add($"%{EscapeLikePattern(rawValue)}%");
        }

        void AddEquals(string columnSql, object value)
        {
            conditions.Add(string.Format(CultureInfo.InvariantCulture, columnSql, parameters.Count));
            parameters.Add(value);
        }

        if (query.Status is { } status)
            AddEquals("Status = {{{0}}}", status.ToString());
        if (!string.IsNullOrWhiteSpace(query.DocumentContains))
            AddLike("Document LIKE {{{0}}} ESCAPE '\\'", query.DocumentContains.Trim());
        if (!string.IsNullOrWhiteSpace(query.LicensePlateContains))
            AddLike("LicensePlate LIKE {{{0}}} ESCAPE '\\'", query.LicensePlateContains.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(query.NumberContains))
            AddLike("Number LIKE {{{0}}} ESCAPE '\\'", query.NumberContains.Trim().ToUpperInvariant());

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

    public async Task RemoveAsync(Policy policy, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _db.Policies.Remove(policy);
        await _db.SaveChangesAsync(ct);
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
