using System.Linq.Expressions;

namespace FaJuan.Api.Application.Common;

public static class QueryablePagingExtensions
{
    public static IQueryable<T> ApplyLegacyPaging<T>(
        this IQueryable<T> source,
        int pageIndex,
        int pageSize,
        Expression<Func<T, long>> orderByDescendingKey)
    {
        var normalizedPageIndex = pageIndex > 0 ? pageIndex : 1;
        var normalizedPageSize = pageSize > 0 ? pageSize : 20;
        var takeCount = normalizedPageIndex * normalizedPageSize;

        // Emulate paging with nested TOP queries so SQL Server 2008 R2 does not require OFFSET/FETCH.
        return source
            .OrderByDescending(orderByDescendingKey)
            .Take(takeCount)
            .OrderBy(orderByDescendingKey)
            .Take(normalizedPageSize)
            .OrderByDescending(orderByDescendingKey);
    }
}
