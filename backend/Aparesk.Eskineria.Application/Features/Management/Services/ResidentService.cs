using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Persistence.Features.Management.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Application.Features.Management.Services;

public sealed class ResidentService : IResidentService
{
    private readonly ISiteRepository _siteRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly ISiteResidentRepository _residentRepository;
    private readonly ICurrentUserService _currentUserService;

    public ResidentService(
        ISiteRepository siteRepository,
        IUnitRepository unitRepository,
        ISiteResidentRepository residentRepository,
        ICurrentUserService currentUserService)
    {
        _siteRepository = siteRepository;
        _unitRepository = unitRepository;
        _residentRepository = residentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<ResidentListItemDto>> GetPagedAsync(GetResidentsRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var searchTerm = NormalizeSearchTerm(request.SearchTerm);

        var query = _residentRepository.Query()
            .Include(resident => resident.Site)
            .Include(resident => resident.Unit)
                .ThenInclude(unit => unit!.SiteBlock)
            .Where(resident =>
                (request.IncludeArchived || !resident.IsArchived) &&
                (!request.SiteId.HasValue || resident.SiteId == request.SiteId.Value) &&
                (!request.UnitId.HasValue || resident.UnitId == request.UnitId.Value) &&
                (!request.Type.HasValue || resident.Type == request.Type.Value) &&
                (!request.IsActive.HasValue || resident.IsActive == request.IsActive.Value) &&
                (searchTerm == null ||
                    resident.FirstName.Contains(searchTerm) ||
                    resident.LastName.Contains(searchTerm) ||
                    (resident.IdentityNumber != null && resident.IdentityNumber.Contains(searchTerm)) ||
                    (resident.Phone != null && resident.Phone.Contains(searchTerm)) ||
                    (resident.Email != null && resident.Email.Contains(searchTerm)) ||
                    resident.Site.Name.Contains(searchTerm) ||
                    (resident.Unit != null && resident.Unit.Number.Contains(searchTerm)) ||
                    (resident.Unit != null && resident.Unit.SiteBlock != null && resident.Unit.SiteBlock.Name.Contains(searchTerm))));

        var page = await query
            .OrderBy(resident => resident.Site.Name)
            .ThenBy(resident => resident.LastName)
            .ThenBy(resident => resident.FirstName)
            .ToPaginateAsync(pageNumber - 1, pageSize, cancellationToken);

        var items = page.Items.Select(MapListItem).ToList();
        return new PagedResponse<ResidentListItemDto>(items, page.Index, page.Size, page.Count, page.Pages, page.HasPrevious, page.HasNext);
    }

    public async Task<DataResponse<ResidentDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resident = await GetTrackedResidentAsync(id, asNoTracking: true, cancellationToken);
        return DataResponse<ResidentDetailDto>.Succeed(MapDetail(resident));
    }

    public async Task<DataResponse<ResidentDetailDto>> CreateAsync(CreateResidentRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(request.SiteId, cancellationToken);
        await EnsureUnitBelongsToSiteAsync(request.SiteId, request.UnitId, cancellationToken);

        var now = DateTime.UtcNow;
        var resident = new SiteResident
        {
            Id = Guid.NewGuid(),
            SiteId = request.SiteId,
            UnitId = request.UnitId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IdentityNumber = TrimOrNull(request.IdentityNumber),
            Type = request.Type,
            Phone = TrimOrNull(request.Phone),
            Email = TrimOrNull(request.Email),
            Occupation = TrimOrNull(request.Occupation),
            EmergencyContactName = TrimOrNull(request.EmergencyContactName),
            EmergencyContactPhone = TrimOrNull(request.EmergencyContactPhone),
            MoveInDate = request.MoveInDate,
            MoveOutDate = request.MoveOutDate,
            KvkkConsentGiven = request.KvkkConsentGiven,
            CommunicationConsentGiven = request.CommunicationConsentGiven,
            Notes = TrimOrNull(request.Notes),
            IsActive = request.IsActive,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = _currentUserService.UserId,
            UpdatedByUserId = _currentUserService.UserId
        };

        await _residentRepository.AddAsync(resident, cancellationToken);
        await _residentRepository.SaveChangesAsync(cancellationToken);

        resident = await GetTrackedResidentAsync(resident.Id, asNoTracking: true, cancellationToken);
        return DataResponse<ResidentDetailDto>.Succeed(MapDetail(resident), "Resident created successfully.", StatusCodes.Status201Created);
    }

    public async Task<DataResponse<ResidentDetailDto>> UpdateAsync(Guid id, UpdateResidentRequest request, CancellationToken cancellationToken = default)
    {
        var resident = await GetTrackedResidentAsync(id, asNoTracking: false, cancellationToken);
        await EnsureUnitBelongsToSiteAsync(resident.SiteId, request.UnitId, cancellationToken);

        resident.UnitId = request.UnitId;
        resident.FirstName = request.FirstName.Trim();
        resident.LastName = request.LastName.Trim();
        resident.IdentityNumber = TrimOrNull(request.IdentityNumber);
        resident.Type = request.Type;
        resident.Phone = TrimOrNull(request.Phone);
        resident.Email = TrimOrNull(request.Email);
        resident.Occupation = TrimOrNull(request.Occupation);
        resident.EmergencyContactName = TrimOrNull(request.EmergencyContactName);
        resident.EmergencyContactPhone = TrimOrNull(request.EmergencyContactPhone);
        resident.MoveInDate = request.MoveInDate;
        resident.MoveOutDate = request.MoveOutDate;
        resident.KvkkConsentGiven = request.KvkkConsentGiven;
        resident.CommunicationConsentGiven = request.CommunicationConsentGiven;
        resident.Notes = TrimOrNull(request.Notes);
        resident.IsActive = request.IsActive;
        resident.UpdatedAtUtc = DateTime.UtcNow;
        resident.UpdatedByUserId = _currentUserService.UserId;

        await _residentRepository.SaveChangesAsync(cancellationToken);
        resident = await GetTrackedResidentAsync(id, asNoTracking: true, cancellationToken);
        return DataResponse<ResidentDetailDto>.Succeed(MapDetail(resident), "Resident updated successfully.");
    }

    public async Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resident = await GetTrackedResidentAsync(id, asNoTracking: false, cancellationToken);
        resident.IsArchived = true;
        resident.IsActive = false;
        resident.ArchivedAtUtc = DateTime.UtcNow;
        resident.UpdatedAtUtc = resident.ArchivedAtUtc.Value;
        resident.UpdatedByUserId = _currentUserService.UserId;

        await _residentRepository.SaveChangesAsync(cancellationToken);
        return Response.Succeed("Resident archived successfully.");
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

    private async Task EnsureUnitBelongsToSiteAsync(Guid siteId, Guid? unitId, CancellationToken cancellationToken)
    {
        if (!unitId.HasValue)
        {
            return;
        }

        var exists = await _unitRepository.Query()
            .AnyAsync(unit => unit.Id == unitId.Value && unit.SiteId == siteId && !unit.IsArchived, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Unit not found in this site.");
        }
    }

    private async Task<SiteResident> GetTrackedResidentAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var resident = await _residentRepository.Query(asNoTracking)
            .Include(entity => entity.Site)
            .Include(entity => entity.Unit)
                .ThenInclude(unit => unit!.SiteBlock)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return resident ?? throw new KeyNotFoundException("Resident not found.");
    }

    private static ResidentListItemDto MapListItem(SiteResident resident) => new()
    {
        Id = resident.Id,
        SiteId = resident.SiteId,
        SiteName = resident.Site.Name,
        UnitId = resident.UnitId,
        UnitNumber = resident.Unit?.Number,
        BlockName = resident.Unit?.SiteBlock?.Name,
        FirstName = resident.FirstName,
        LastName = resident.LastName,
        FullName = GetFullName(resident),
        Type = resident.Type,
        Phone = resident.Phone,
        Email = resident.Email,
        MoveInDate = resident.MoveInDate,
        MoveOutDate = resident.MoveOutDate,
        IsActive = resident.IsActive,
        IsArchived = resident.IsArchived,
        UpdatedAtUtc = resident.UpdatedAtUtc
    };

    private static ResidentDetailDto MapDetail(SiteResident resident) => new()
    {
        Id = resident.Id,
        SiteId = resident.SiteId,
        SiteName = resident.Site.Name,
        UnitId = resident.UnitId,
        UnitNumber = resident.Unit?.Number,
        BlockName = resident.Unit?.SiteBlock?.Name,
        FirstName = resident.FirstName,
        LastName = resident.LastName,
        FullName = GetFullName(resident),
        Type = resident.Type,
        Phone = resident.Phone,
        Email = resident.Email,
        MoveInDate = resident.MoveInDate,
        MoveOutDate = resident.MoveOutDate,
        IsActive = resident.IsActive,
        IsArchived = resident.IsArchived,
        UpdatedAtUtc = resident.UpdatedAtUtc,
        IdentityNumber = resident.IdentityNumber,
        Occupation = resident.Occupation,
        EmergencyContactName = resident.EmergencyContactName,
        EmergencyContactPhone = resident.EmergencyContactPhone,
        KvkkConsentGiven = resident.KvkkConsentGiven,
        CommunicationConsentGiven = resident.CommunicationConsentGiven,
        Notes = resident.Notes,
        CreatedAtUtc = resident.CreatedAtUtc,
        ArchivedAtUtc = resident.ArchivedAtUtc,
        CreatedByUserId = resident.CreatedByUserId,
        UpdatedByUserId = resident.UpdatedByUserId
    };

    private static string GetFullName(SiteResident resident) => $"{resident.FirstName} {resident.LastName}".Trim();
    private static string? NormalizeSearchTerm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
