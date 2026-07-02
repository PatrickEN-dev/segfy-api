namespace Segfy.Domain.Policies.Abstractions;

public sealed record PolicyListQuery(
    int Page,
    int PageSize,
    PolicyStatus? Status,
    string? DocumentContains,
    string? LicensePlateContains,
    string? NumberContains,
    PolicySortField SortBy,
    SortDirection SortDir);

public enum PolicySortField
{
    CreatedAt,
    CoverageEnd,
    Premium
}

public enum SortDirection
{
    Asc,
    Desc
}
