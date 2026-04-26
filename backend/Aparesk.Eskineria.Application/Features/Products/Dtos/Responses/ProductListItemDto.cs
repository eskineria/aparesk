namespace Aparesk.Eskineria.Application.Features.Products.Dtos.Responses;

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
