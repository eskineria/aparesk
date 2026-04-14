using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Repository.Paging;

public class PagedList<T> : IPaginate<T>
{
    public int From { get; }
    public int Index { get; }
    public int Size { get; }
    public int Count { get; }
    public int Pages { get; }
    public IList<T> Items { get; }
    public bool HasPrevious => Index > 0;
    public bool HasNext => Index + 1 < Pages;

    public PagedList(IEnumerable<T> source, int index, int size, int count)
    {
        Index = Math.Max(0, index);
        Size = Math.Max(1, size);
        Count = Math.Max(0, count);
        Pages = Count == 0 ? 0 : (int)Math.Ceiling(Count / (double)Size);
        Items = source as IList<T> ?? source.ToList();
        From = Count == 0 ? 0 : Index * Size;
    }

    internal PagedList()
    {
        Items = new T[0];
    }
}

public static class QueryableExtensions
{
    public static async Task<IPaginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source, 
        int index, 
        int size, 
        CancellationToken cancellationToken = default)
    {
        index = Math.Max(0, index);
        size = Math.Max(1, size);

        var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await source.Skip(index * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<T>(items, index, size, count);
    }
}
