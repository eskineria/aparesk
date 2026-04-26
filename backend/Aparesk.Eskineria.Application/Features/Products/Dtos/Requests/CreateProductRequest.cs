namespace Aparesk.Eskineria.Application.Features.Products.Dtos.Requests;

public class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

