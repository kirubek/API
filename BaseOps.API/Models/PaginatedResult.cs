namespace BaseOps.API.Models;

public sealed record PaginatedResult<T>(IReadOnlyCollection<T> Items, int TotalCount, int Page, int PageSize, int TotalPages)
{
    public int CurrentPage => Page;
}

public static class ApiResults
{
    public static PaginatedResult<T> Page<T>(IReadOnlyCollection<T> items, int totalCount, int page, int pageSize)
    {
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PaginatedResult<T>(items, totalCount, page, pageSize, totalPages);
    }

    public static PaginatedResult<T> EmptyPage<T>(int page = 1, int pageSize = 20) => Page(Array.Empty<T>(), 0, page, pageSize);
}
