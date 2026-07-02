using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.DTOs;

public sealed record ListPoliciesInput(
    int Page,
    int PageSize,
    PolicyStatus? Status,
    string? Document,
    string? LicensePlate,
    string? Number,
    PolicySortField SortBy,
    SortDirection SortDir);
