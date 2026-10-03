namespace CMS.Application.Common.Paging;

public static class PageSlice
{
    public static PagedResult<T> FromList<T>(
        IReadOnlyList<T> source,
        int page,
        int pageSize = PagedRequest.DefaultPageSize)
    {
        var request = new PagedRequest { Page = page, PageSize = pageSize };
        var items = source
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .ToList();

        return new PagedResult<T>(items, source.Count, request.NormalizedPage, request.NormalizedPageSize);
    }

    public static void ApplyToViewBag(
        dynamic viewBag,
        int page,
        int totalCount,
        int pageSize = PagedRequest.DefaultPageSize)
    {
        var request = new PagedRequest { Page = page, PageSize = pageSize };
        viewBag.Page = request.NormalizedPage;
        viewBag.PageSize = request.NormalizedPageSize;
        viewBag.TotalCount = totalCount;
        viewBag.TotalPages = request.NormalizedPageSize <= 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.NormalizedPageSize);
    }

    public static void ApplyToViewBag<T>(dynamic viewBag, PagedResult<T> result)
    {
        viewBag.Page = result.Page;
        viewBag.PageSize = result.PageSize;
        viewBag.TotalCount = result.TotalCount;
        viewBag.TotalPages = result.TotalPages;
    }
}
