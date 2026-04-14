using Eskineria.Application.Features.Products.Abstractions;
using Eskineria.Application.Features.Products.Dtos.Requests;
using Eskineria.Application.Features.Products.Dtos.Responses;
using Eskineria.Application.Features.Products.Specifications;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Repository.Specification;
using Eskineria.Core.Shared.Response;
using Eskineria.Domain.Entities;
using Eskineria.Persistence.Features.Products.Abstractions;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Eskineria.Application.Features.Products.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ProductService> _localizer;

    public ProductService(
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IStringLocalizer<ProductService> localizer)
    {
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _localizer = localizer;
    }

    public async Task<PagedResponse<ProductListItemDto>> GetPagedAsync(GetProductsRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(1, request.PageNumber);
        var normalizedPageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;
        var specification = new ProductPagedSpecification(request, skip, normalizedPageSize);

        var page = await _productRepository.GetPagedListAsync(specification, cancellationToken);
        var mappedItems = page.Items.Select(product => _mapper.Map<ProductListItemDto>(product)).ToList();

        return new PagedResponse<ProductListItemDto>(
            mappedItems,
            page.Index,
            page.Size,
            page.Count,
            page.Pages,
            page.HasPrevious,
            page.HasNext,
            _localizer["ProductsRetrievedSuccessfully"].Value);
    }

    public async Task<DataResponse<ProductDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var specification = new ProductByIdSpecification(id);
        var product = (await _productRepository.GetListAsync(specification, cancellationToken))
            .FirstOrDefault();
        if (product == null)
        {
            throw new KeyNotFoundException(_localizer["ProductNotFound"].Value);
        }

        return DataResponse<ProductDetailDto>.Succeed(_mapper.Map<ProductDetailDto>(product), _localizer["ProductRetrievedSuccessfully"].Value);
    }

    public async Task<DataResponse<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = _mapper.Map<Product>(request);
        var now = DateTime.UtcNow;
        product.Id = Guid.NewGuid();
        product.IsArchived = false;
        product.CreatedAtUtc = now;
        product.UpdatedAtUtc = now;
        product.CreatedByUserId = _currentUserService.UserId;
        product.UpdatedByUserId = _currentUserService.UserId;

        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return DataResponse<ProductDetailDto>.Succeed(
            _mapper.Map<ProductDetailDto>(product),
            _localizer["ProductCreatedSuccessfully"].Value,
            StatusCodes.Status201Created);
    }

    public async Task<DataResponse<ProductDetailDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var specification = new ProductByIdSpecification(id);
        var product = await SpecificationEvaluator<Product>
            .GetQuery(_productRepository.Query(asNoTracking: false), specification)
            .FirstOrDefaultAsync(cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException(_localizer["ProductNotFound"].Value);
        }

        _mapper.Map(request, product);
        product.UpdatedAtUtc = DateTime.UtcNow;
        product.UpdatedByUserId = _currentUserService.UserId;

        await _productRepository.SaveChangesAsync(cancellationToken);
        return DataResponse<ProductDetailDto>.Succeed(_mapper.Map<ProductDetailDto>(product), _localizer["ProductUpdatedSuccessfully"].Value);
    }

    public async Task<Response> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var specification = new ProductByIdSpecification(id);
        var product = await SpecificationEvaluator<Product>
            .GetQuery(_productRepository.Query(asNoTracking: false), specification)
            .FirstOrDefaultAsync(cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException(_localizer["ProductNotFound"].Value);
        }

        await _productRepository.DeleteAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);
        return Response.Succeed(_localizer["ProductDeletedSuccessfully"].Value);
    }
}
