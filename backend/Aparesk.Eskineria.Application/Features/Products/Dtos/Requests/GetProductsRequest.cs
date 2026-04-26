namespace Aparesk.Eskineria.Application.Features.Products.Dtos.Requests;

public class GetProductsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeArchived { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Currency { get; set; }
}

