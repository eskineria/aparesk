using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Persistence.Features.Management.Abstractions;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Application.Features.Management.Services;

public sealed class UnitService : IUnitService
{
    private readonly ISiteRepository _siteRepository;
    private readonly ISiteBlockRepository _blockRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<UnitService> _localizer;

    public UnitService(
        ISiteRepository siteRepository,
        ISiteBlockRepository blockRepository,
        IUnitRepository unitRepository,
        ICurrentUserService currentUserService,
        IStringLocalizer<UnitService> localizer)
    {
        _siteRepository = siteRepository;
        _blockRepository = blockRepository;
        _unitRepository = unitRepository;
        _currentUserService = currentUserService;
        _localizer = localizer;
    }

    public async Task<PagedResponse<UnitListItemDto>> GetPagedAsync(GetUnitsRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var searchTerm = NormalizeSearchTerm(request.SearchTerm);

        var query = _unitRepository.Query()
            .Include(unit => unit.Site)
            .Include(unit => unit.SiteBlock)
            .Where(unit =>
                (request.IncludeArchived || !unit.IsArchived) &&
                (!request.SiteId.HasValue || unit.SiteId == request.SiteId.Value) &&
                (!request.SiteBlockId.HasValue || unit.SiteBlockId == request.SiteBlockId.Value) &&
                (!request.IsActive.HasValue || unit.IsActive == request.IsActive.Value) &&
                (searchTerm == null ||
                    unit.Number.Contains(searchTerm) ||
                    (unit.DoorNumber != null && unit.DoorNumber.Contains(searchTerm)) ||
                    unit.Site.Name.Contains(searchTerm) ||
                    (unit.SiteBlock != null && unit.SiteBlock.Name.Contains(searchTerm))));

        var page = await query
            .OrderBy(unit => unit.Site.Name)
            .ThenBy(unit => unit.SiteBlock == null ? string.Empty : unit.SiteBlock.Name)
            .ThenBy(unit => unit.Number.Length)
            .ThenBy(unit => unit.Number)
            .ToPaginateAsync(pageNumber - 1, pageSize, cancellationToken);

        var items = page.Items.Select(MapListItem).ToList();
        return new PagedResponse<UnitListItemDto>(items, page.Index, page.Size, page.Count, page.Pages, page.HasPrevious, page.HasNext);
    }

    public async Task<DataResponse<UnitDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await GetTrackedUnitAsync(id, asNoTracking: true, cancellationToken);
        return DataResponse<UnitDetailDto>.Succeed(MapDetail(unit));
    }

    public async Task<DataResponse<UnitDetailDto>> CreateAsync(CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(request.SiteId, cancellationToken);
        await EnsureBlockBelongsToSiteAsync(request.SiteId, request.SiteBlockId, cancellationToken);
        var number = NormalizeNumber(request.Number);
        await EnsureNumberIsUniqueAsync(request.SiteId, request.SiteBlockId, number, excludingId: null, cancellationToken);

        var now = DateTime.UtcNow;
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            SiteId = request.SiteId,
            SiteBlockId = request.SiteBlockId,
            Number = number,
            DoorNumber = TrimOrNull(request.DoorNumber),
            Type = request.Type,
            FloorNumber = request.FloorNumber,
            GrossAreaSquareMeters = request.GrossAreaSquareMeters,
            NetAreaSquareMeters = request.NetAreaSquareMeters,
            LandShare = request.LandShare,
            Notes = TrimOrNull(request.Notes),
            IsActive = request.IsActive,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = _currentUserService.UserId,
            UpdatedByUserId = _currentUserService.UserId
        };

        await _unitRepository.AddAsync(unit, cancellationToken);
        await _unitRepository.SaveChangesAsync(cancellationToken);

        unit = await GetTrackedUnitAsync(unit.Id, asNoTracking: true, cancellationToken);
        return DataResponse<UnitDetailDto>.Succeed(MapDetail(unit), _localizer["UnitCreatedSuccessfully"].Value, StatusCodes.Status201Created);
    }

    public async Task<DataResponse<UnitDetailDto>> UpdateAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await GetTrackedUnitAsync(id, asNoTracking: false, cancellationToken);
        await EnsureBlockBelongsToSiteAsync(unit.SiteId, request.SiteBlockId, cancellationToken);
        var number = NormalizeNumber(request.Number);
        await EnsureNumberIsUniqueAsync(unit.SiteId, request.SiteBlockId, number, id, cancellationToken);

        unit.SiteBlockId = request.SiteBlockId;
        unit.Number = number;
        unit.DoorNumber = TrimOrNull(request.DoorNumber);
        unit.Type = request.Type;
        unit.FloorNumber = request.FloorNumber;
        unit.GrossAreaSquareMeters = request.GrossAreaSquareMeters;
        unit.NetAreaSquareMeters = request.NetAreaSquareMeters;
        unit.LandShare = request.LandShare;
        unit.Notes = TrimOrNull(request.Notes);
        unit.IsActive = request.IsActive;
        unit.UpdatedAtUtc = DateTime.UtcNow;
        unit.UpdatedByUserId = _currentUserService.UserId;

        await _unitRepository.SaveChangesAsync(cancellationToken);
        unit = await GetTrackedUnitAsync(id, asNoTracking: true, cancellationToken);
        return DataResponse<UnitDetailDto>.Succeed(MapDetail(unit), _localizer["UnitUpdatedSuccessfully"].Value);
    }

    public async Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await GetTrackedUnitAsync(id, asNoTracking: false, cancellationToken);
        unit.IsArchived = true;
        unit.IsActive = false;
        unit.ArchivedAtUtc = DateTime.UtcNow;
        unit.UpdatedAtUtc = unit.ArchivedAtUtc.Value;
        unit.UpdatedByUserId = _currentUserService.UserId;

        await _unitRepository.SaveChangesAsync(cancellationToken);
        return Response.Succeed(_localizer["UnitArchivedSuccessfully"].Value);
    }

    private async Task EnsureSiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var exists = await _siteRepository.Query()
            .AnyAsync(site => site.Id == siteId && !site.IsArchived, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException(_localizer["SiteNotFound"].Value);
        }
    }

    private async Task EnsureBlockBelongsToSiteAsync(Guid siteId, Guid? blockId, CancellationToken cancellationToken)
    {
        if (!blockId.HasValue)
        {
            return;
        }

        var exists = await _blockRepository.Query()
            .AnyAsync(block => block.Id == blockId.Value && block.SiteId == siteId && !block.IsArchived, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException(_localizer["BlockNotFoundInSite"].Value);
        }
    }

    private async Task<Unit> GetTrackedUnitAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var unit = await _unitRepository.Query(asNoTracking)
            .Include(entity => entity.Site)
            .Include(entity => entity.SiteBlock)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return unit ?? throw new KeyNotFoundException(_localizer["UnitNotFound"].Value);
    }

    private async Task EnsureNumberIsUniqueAsync(Guid siteId, Guid? blockId, string number, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await _unitRepository.Query()
            .AnyAsync(unit =>
                unit.SiteId == siteId &&
                unit.SiteBlockId == blockId &&
                unit.Number == number &&
                (!excludingId.HasValue || unit.Id != excludingId.Value),
                cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(_localizer["UnitNumberAlreadyExists"].Value);
        }
    }

    private static UnitListItemDto MapListItem(Unit unit) => new()
    {
        Id = unit.Id,
        SiteId = unit.SiteId,
        SiteName = unit.Site.Name,
        SiteBlockId = unit.SiteBlockId,
        BlockName = unit.SiteBlock?.Name,
        Number = unit.Number,
        DoorNumber = unit.DoorNumber,
        Type = unit.Type,
        FloorNumber = unit.FloorNumber,
        GrossAreaSquareMeters = unit.GrossAreaSquareMeters,
        NetAreaSquareMeters = unit.NetAreaSquareMeters,
        LandShare = unit.LandShare,
        IsActive = unit.IsActive,
        IsArchived = unit.IsArchived,
        UpdatedAtUtc = unit.UpdatedAtUtc
    };

    private static UnitDetailDto MapDetail(Unit unit) => new()
    {
        Id = unit.Id,
        SiteId = unit.SiteId,
        SiteName = unit.Site.Name,
        SiteBlockId = unit.SiteBlockId,
        BlockName = unit.SiteBlock?.Name,
        Number = unit.Number,
        DoorNumber = unit.DoorNumber,
        Type = unit.Type,
        FloorNumber = unit.FloorNumber,
        GrossAreaSquareMeters = unit.GrossAreaSquareMeters,
        NetAreaSquareMeters = unit.NetAreaSquareMeters,
        LandShare = unit.LandShare,
        IsActive = unit.IsActive,
        IsArchived = unit.IsArchived,
        UpdatedAtUtc = unit.UpdatedAtUtc,
        Notes = unit.Notes,
        CreatedAtUtc = unit.CreatedAtUtc,
        ArchivedAtUtc = unit.ArchivedAtUtc,
        CreatedByUserId = unit.CreatedByUserId,
        UpdatedByUserId = unit.UpdatedByUserId
    };

    private static string NormalizeNumber(string number) => number.Trim().ToUpperInvariant();
    private static string? NormalizeSearchTerm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
