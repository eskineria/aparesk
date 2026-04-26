namespace Aparesk.Eskineria.Application.Features.Products.Dtos.Responses;

public class ProductDetailDto : ProductListItemDto
{
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
