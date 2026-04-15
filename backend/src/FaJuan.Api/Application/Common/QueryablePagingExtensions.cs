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

    public static IQueryable<T> ApplyLegacyPaging<T, TPrimary, TSecondary>(
        this IQueryable<T> source,
        int pageIndex,
        int pageSize,
        Expression<Func<T, TPrimary>> primaryOrderKey,
        bool primaryDescending,
        Expression<Func<T, TSecondary>> secondaryOrderKey,
        bool secondaryDescending)
    {
        var normalizedPageIndex = pageIndex > 0 ? pageIndex : 1;
        var normalizedPageSize = pageSize > 0 ? pageSize : 20;
        var takeCount = normalizedPageIndex * normalizedPageSize;

        var ordered = ApplyOrder(source, primaryOrderKey, primaryDescending, secondaryOrderKey, secondaryDescending);
        var reversed = ApplyOrder(ordered.Take(takeCount), primaryOrderKey, !primaryDescending, secondaryOrderKey, !secondaryDescending);

        return ApplyOrder(
            reversed.Take(normalizedPageSize),
            primaryOrderKey,
            primaryDescending,
            secondaryOrderKey,
            secondaryDescending);

        IOrderedQueryable<T> ApplyOrder(
            IQueryable<T> query,
            Expression<Func<T, TPrimary>> primaryKey,
            bool isPrimaryDescending,
            Expression<Func<T, TSecondary>> secondaryKey,
            bool isSecondaryDescending)
        {
            var primaryOrdered = isPrimaryDescending
                ? query.OrderByDescending(primaryKey)
                : query.OrderBy(primaryKey);

            return isSecondaryDescending
                ? primaryOrdered.ThenByDescending(secondaryKey)
                : primaryOrdered.ThenBy(secondaryKey);
        }
    }
}
