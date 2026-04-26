namespace Aparesk.Eskineria.Core.Shared.Configuration;

public class PagingOptions
{
    private const int MinimumPageSize = 1;
    private const int FallbackDefaultPageSize = 20;
    private const int FallbackMaxPageSize = 200;

    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 200;

    public int NormalizePageNumber(int pageNumber)
    {
        return pageNumber <= 0 ? 1 : pageNumber;
    }

    public int NormalizePageSize(int requestedPageSize)
    {
        var effectiveMaxPageSize = MaxPageSize < MinimumPageSize ? FallbackMaxPageSize : MaxPageSize;
        var effectiveDefaultPageSize = DefaultPageSize < MinimumPageSize
            ? FallbackDefaultPageSize
            : DefaultPageSize > effectiveMaxPageSize
                ? effectiveMaxPageSize
                : DefaultPageSize;

        if (requestedPageSize <= 0)
        {
            return effectiveDefaultPageSize;
        }

        return requestedPageSize > effectiveMaxPageSize
            ? effectiveMaxPageSize
            : requestedPageSize;
    }
}
