using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Infrastructure.Persistence.Repositories;

public sealed class PolicyRepository : IPolicyRepository
{
    private readonly SegfyDbContext _db;

    public PolicyRepository(SegfyDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Policy policy, CancellationToken ct)
    {
        _db.Policies.Add(policy);
        await _db.SaveChangesAsync(ct);
    }

    public Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct) =>
        _db.Policies
            .Include(p => p.StatusHistory)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Policy> Items, int Total)> ListAsync(
        PolicyListQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var where = new StringBuilder();
        var parameters = new List<object>();

        void AppendCondition(string columnCondition, object value)
        {
            where.Append(where.Length == 0 ? " WHERE " : " AND ");
            where.AppendFormat(CultureInfo.InvariantCulture, columnCondition, parameters.Count);
            parameters.Add(value);
        }

        if (query.Status is { } status)
            AppendCondition("Status = {{{0}}}", status.ToString());

        if (!string.IsNullOrWhiteSpace(query.DocumentContains))
            AppendCondition("Document LIKE {{{0}}}", $"%{query.DocumentContains.Trim()}%");

        if (!string.IsNullOrWhiteSpace(query.LicensePlateContains))
            AppendCondition("LicensePlate LIKE {{{0}}}", $"%{query.LicensePlateContains.Trim().ToUpperInvariant()}%");

        if (!string.IsNullOrWhiteSpace(query.NumberContains))
            AppendCondition("Number LIKE {{{0}}}", $"%{query.NumberContains.Trim().ToUpperInvariant()}%");

        var orderByColumn = query.SortBy switch
        {
            PolicySortField.CoverageEnd => "CoverageEnd",
            PolicySortField.Premium => "PremiumAmount",
            _ => "CreatedAt",
        };
        var orderByDir = query.SortDir == SortDirection.Asc ? "ASC" : "DESC";

        var whereClause = where.ToString();
        var countSql = $"SELECT COUNT(*) FROM Policies{whereClause}";
        var total = await ExecuteScalarIntAsync(countSql, parameters, ct);

        var pageParamIndex = parameters.Count;
        var offsetParamIndex = parameters.Count + 1;
        parameters.Add(query.PageSize);
        parameters.Add((query.Page - 1) * query.PageSize);

        var listSql = $"SELECT * FROM Policies{whereClause} ORDER BY {orderByColumn} {orderByDir} LIMIT {{{pageParamIndex}}} OFFSET {{{offsetParamIndex}}}";

        var items = await _db.Policies
            .FromSqlRaw(listSql, parameters.ToArray())
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }

    private async Task<int> ExecuteScalarIntAsync(
        string sql, IReadOnlyList<object> parameters, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        var mustOpen = connection.State != System.Data.ConnectionState.Open;
        if (mustOpen)
            await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = ReplacePositionalPlaceholders(sql, parameters.Count);
            for (var i = 0; i < parameters.Count; i++)
            {
                var p = command.CreateParameter();
                p.ParameterName = $"@p{i}";
                p.Value = parameters[i];
                command.Parameters.Add(p);
            }

            var result = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (mustOpen)
                await connection.CloseAsync();
        }
    }

    private static string ReplacePositionalPlaceholders(string sql, int count)
    {
        var result = sql;
        for (var i = 0; i < count; i++)
            result = result.Replace($"{{{i}}}", $"@p{i}", StringComparison.Ordinal);
        return result;
    }

    public Task UpdateAsync(Policy policy, CancellationToken ct) =>
        _db.SaveChangesAsync(ct);

    public async Task RemoveAsync(Policy policy, CancellationToken ct)
    {
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
                "Ativa", todayStr, cutoffStr)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsActiveByPlateAsync(
        string plateValue, Guid? excludePolicyId, CancellationToken ct)
    {
        var sql = new StringBuilder("SELECT COUNT(*) FROM Policies WHERE Status = {0} AND LicensePlate = {1}");
        var parameters = new List<object> { "Ativa", plateValue };
        if (excludePolicyId is { } id)
        {
            sql.Append(" AND Id <> {2}");
            parameters.Add(id.ToString());
        }
        var count = await ExecuteScalarIntAsync(sql.ToString(), parameters, ct);
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
                "Ativa", todayStr)
            .Include(p => p.StatusHistory)
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
}
