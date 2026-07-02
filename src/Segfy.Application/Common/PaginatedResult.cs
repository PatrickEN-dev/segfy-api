namespace Segfy.Application.Common;

public sealed record PaginatedResult<T>(IReadOnlyList<T> Data, int Page, int PageSize, int Total);
