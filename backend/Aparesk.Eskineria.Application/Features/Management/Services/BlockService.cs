using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Domain.Enums;
using Aparesk.Eskineria.Persistence.Features.Management.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Application.Features.Management.Services;

public sealed class BlockService : IBlockService
{
    private readonly ISiteRepository _siteRepository;
    private readonly ISiteBlockRepository _blockRepository;
    private readonly ICurrentUserService _currentUserService;

    public BlockService(
        ISiteRepository siteRepository,
        ISiteBlockRepository blockRepository,
        ICurrentUserService currentUserService)
    {
        _siteRepository = siteRepository;
        _blockRepository = blockRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<BlockListItemDto>> GetPagedAsync(GetBlocksRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var searchTerm = NormalizeSearchTerm(request.SearchTerm);

        var query = _blockRepository.Query()
            .Include(block => block.Site)
            .Include(block => block.Units)
            .Where(block =>
                (request.IncludeArchived || !block.IsArchived) &&
                (!request.SiteId.HasValue || block.SiteId == request.SiteId.Value) &&
                (!request.IsActive.HasValue || block.IsActive == request.IsActive.Value) &&
                (searchTerm == null ||
                    block.Name.Contains(searchTerm) ||
                    block.Site.Name.Contains(searchTerm)));

        var page = await query
            .OrderBy(block => block.Site.Name)
            .ThenBy(block => block.Name)
            .ToPaginateAsync(pageNumber - 1, pageSize, cancellationToken);

        var items = page.Items.Select(MapListItem).ToList();
        return new PagedResponse<BlockListItemDto>(items, page.Index, page.Size, page.Count, page.Pages, page.HasPrevious, page.HasNext);
    }

    public async Task<DataResponse<BlockDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var block = await GetTrackedBlockAsync(id, asNoTracking: true, cancellationToken);
        return DataResponse<BlockDetailDto>.Succeed(MapDetail(block));
    }

    public async Task<DataResponse<BlockDetailDto>> CreateAsync(CreateBlockRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(request.SiteId, cancellationToken);
        var now = DateTime.UtcNow;
        var blockId = Guid.NewGuid();
        var block = new SiteBlock
        {
            Id = blockId,
            SiteId = request.SiteId,
            Code = await GenerateUniqueCodeAsync(request.SiteId, request.Name, cancellationToken),
            Name = request.Name.Trim(),
            FloorCount = request.FloorCount,
            Description = TrimOrNull(request.Description),
            IsActive = request.IsActive,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = _currentUserService.UserId,
            UpdatedByUserId = _currentUserService.UserId
        };

        AddGeneratedUnits(block, request.UnitsPerFloor.GetValueOrDefault(), now);

        await _blockRepository.AddAsync(block, cancellationToken);
        await _blockRepository.SaveChangesAsync(cancellationToken);

        block = await GetTrackedBlockAsync(block.Id, asNoTracking: true, cancellationToken);
        return DataResponse<BlockDetailDto>.Succeed(MapDetail(block), "Block created successfully.", StatusCodes.Status201Created);
    }

    public async Task<DataResponse<BlockDetailDto>> UpdateAsync(Guid id, UpdateBlockRequest request, CancellationToken cancellationToken = default)
    {
        var block = await GetTrackedBlockAsync(id, asNoTracking: false, cancellationToken);

        block.Name = request.Name.Trim();
        block.FloorCount = request.FloorCount;
        block.Description = TrimOrNull(request.Description);
        block.IsActive = request.IsActive;
        block.UpdatedAtUtc = DateTime.UtcNow;
        block.UpdatedByUserId = _currentUserService.UserId;

        await _blockRepository.SaveChangesAsync(cancellationToken);
        return DataResponse<BlockDetailDto>.Succeed(MapDetail(block), "Block updated successfully.");
    }

    public async Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var block = await GetTrackedBlockAsync(id, asNoTracking: false, cancellationToken);
        block.IsArchived = true;
        block.IsActive = false;
        block.ArchivedAtUtc = DateTime.UtcNow;
        block.UpdatedAtUtc = block.ArchivedAtUtc.Value;
        block.UpdatedByUserId = _currentUserService.UserId;

        await _blockRepository.SaveChangesAsync(cancellationToken);
        return Response.Succeed("Block archived successfully.");
    }

    private async Task EnsureSiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var exists = await _siteRepository.Query()
            .AnyAsync(site => site.Id == siteId && !site.IsArchived, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Site not found.");
        }
    }

    private async Task<SiteBlock> GetTrackedBlockAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var block = await _blockRepository.Query(asNoTracking)
            .Include(entity => entity.Site)
            .Include(entity => entity.Units)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return block ?? throw new KeyNotFoundException("Block not found.");
    }

    private async Task<string> GenerateUniqueCodeAsync(Guid siteId, string name, CancellationToken cancellationToken)
    {
        var baseCode = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (baseCode.Length > 10) baseCode = baseCode[..10];
        if (string.IsNullOrEmpty(baseCode)) baseCode = "B";

        var code = baseCode;
        int counter = 1;
        while (await _blockRepository.Query().AnyAsync(b => b.SiteId == siteId && b.Code == code, cancellationToken))
        {
            code = $"{baseCode}{counter++}";
        }
        return code;
    }

    private void AddGeneratedUnits(SiteBlock block, int unitsPerFloor, DateTime now)
    {
        var floorCount = block.FloorCount.GetValueOrDefault();
        var userId = _currentUserService.UserId;
        var unitNumber = 1;

        for (var floor = 1; floor <= floorCount; floor++)
        {
            for (var unit = 1; unit <= unitsPerFloor; unit++)
            {
                var number = unitNumber.ToString();
                block.Units.Add(new Unit
                {
                    Id = Guid.NewGuid(),
                    SiteId = block.SiteId,
                    SiteBlockId = block.Id,
                    Number = number,
                    DoorNumber = number,
                    Type = UnitType.Apartment,
                    FloorNumber = floor,
                    IsActive = true,
                    IsArchived = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId
                });
                unitNumber++;
            }
        }
    }

    private static BlockListItemDto MapListItem(SiteBlock block) => new()
    {
        Id = block.Id,
        SiteId = block.SiteId,
        SiteName = block.Site.Name,
        Name = block.Name,
        FloorCount = block.FloorCount,
        IsActive = block.IsActive,
        IsArchived = block.IsArchived,
        UnitCount = block.Units.Count(unit => !unit.IsArchived),
        UpdatedAtUtc = block.UpdatedAtUtc
    };

    private static BlockDetailDto MapDetail(SiteBlock block) => new()
    {
        Id = block.Id,
        SiteId = block.SiteId,
        SiteName = block.Site.Name,
        Name = block.Name,
        FloorCount = block.FloorCount,
        IsActive = block.IsActive,
        IsArchived = block.IsArchived,
        UnitCount = block.Units.Count(unit => !unit.IsArchived),
        UpdatedAtUtc = block.UpdatedAtUtc,
        Description = block.Description,
        CreatedAtUtc = block.CreatedAtUtc,
        ArchivedAtUtc = block.ArchivedAtUtc,
        CreatedByUserId = block.CreatedByUserId,
        UpdatedByUserId = block.UpdatedByUserId
    };

    private static string? NormalizeSearchTerm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
