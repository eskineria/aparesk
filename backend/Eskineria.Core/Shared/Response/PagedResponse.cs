namespace Eskineria.Core.Shared.Response;

public class PagedResponse<T> : DataResponse<IList<T>>
{
    public int Index { get; set; }
    public int Size { get; set; }
    public int Count { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public PagedResponse()
    {
    }

    public PagedResponse(IList<T> items, int index, int size, int count, int pages, bool hasPrevious, bool hasNext, string message = "Success")
        : base(items, true, message, 200)
    {
        Index = Math.Max(0, index);
        Size = Math.Max(1, size);
        Count = Math.Max(0, count);

        var calculatedPages = Count == 0 ? 0 : (int)Math.Ceiling(Count / (double)Size);
        Pages = Math.Max(pages, calculatedPages);
        HasPrevious = hasPrevious || Index > 0;
        HasNext = hasNext || (Pages > 0 && Index + 1 < Pages);
    }
}
