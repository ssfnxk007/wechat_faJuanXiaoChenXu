namespace FaJuan.Api.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
