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

public sealed class ResidentService : IResidentService
{
    private readonly ISiteRepository _siteRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly ISiteResidentRepository _residentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<ResidentService> _localizer;

    public ResidentService(
        ISiteRepository siteRepository,
        IUnitRepository unitRepository,
        ISiteResidentRepository residentRepository,
        ICurrentUserService currentUserService,
        IStringLocalizer<ResidentService> localizer)
    {
        _siteRepository = siteRepository;
        _unitRepository = unitRepository;
        _residentRepository = residentRepository;
        _currentUserService = currentUserService;
        _localizer = localizer;
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
        await EnsureUnitIsAvailableAsync(request.UnitId, null, cancellationToken);

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
            UpdatedByUserId = _currentUserService.UserId,
            OwnerFirstName = TrimOrNull(request.OwnerFirstName),
            OwnerLastName = TrimOrNull(request.OwnerLastName),
            OwnerPhone = TrimOrNull(request.OwnerPhone),
            HouseholdMembers = request.HouseholdMembers.Select(m => new HouseholdMember
            {
                Id = Guid.NewGuid(),
                FirstName = m.FirstName.Trim(),
                LastName = m.LastName.Trim(),
                Phone = TrimOrNull(m.Phone),
                IdentityNumber = TrimOrNull(m.IdentityNumber),
                Relationship = TrimOrNull(m.Relationship)
            }).ToList()
        };

        await _residentRepository.AddAsync(resident, cancellationToken);
        await _residentRepository.SaveChangesAsync(cancellationToken);

        resident = await GetTrackedResidentAsync(resident.Id, asNoTracking: true, cancellationToken);
        return DataResponse<ResidentDetailDto>.Succeed(MapDetail(resident), _localizer["ResidentCreatedSuccessfully"].Value, StatusCodes.Status201Created);
    }

    public async Task<DataResponse<ResidentDetailDto>> UpdateAsync(Guid id, UpdateResidentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var resident = await GetTrackedResidentAsync(id, asNoTracking: false, cancellationToken);

            await EnsureUnitBelongsToSiteAsync(resident.SiteId, request.UnitId, cancellationToken);
            await EnsureUnitIsAvailableAsync(request.UnitId, id, cancellationToken);

            resident.UnitId = request.UnitId;
            resident.FirstName = request.FirstName.Trim();
            resident.LastName = request.LastName.Trim();
            resident.IdentityNumber = TrimOrNull(request.IdentityNumber);
            resident.Type = request.Type;
            resident.Phone = TrimOrNull(request.Phone);
            resident.Email = TrimOrNull(request.Email);
            resident.Occupation = TrimOrNull(request.Occupation);
            resident.MoveInDate = request.MoveInDate;
            resident.MoveOutDate = request.MoveOutDate;
            resident.KvkkConsentGiven = request.KvkkConsentGiven;
            resident.CommunicationConsentGiven = request.CommunicationConsentGiven;
            resident.Notes = TrimOrNull(request.Notes);
            resident.IsActive = request.IsActive;
            resident.OwnerFirstName = TrimOrNull(request.OwnerFirstName);
            resident.OwnerLastName = TrimOrNull(request.OwnerLastName);
            resident.OwnerPhone = TrimOrNull(request.OwnerPhone);

            // Update Household Members (Sync)
            // 1. Remove all existing members from the tracked collection
            var existingMembers = resident.HouseholdMembers.ToList();
            foreach (var existing in existingMembers)
            {
                resident.HouseholdMembers.Remove(existing);
            }

            // 2. Add new ones from request
            foreach (var m in request.HouseholdMembers)
            {
                var firstName = m.FirstName?.Trim();
                var lastName = m.LastName?.Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    continue;

                resident.HouseholdMembers.Add(new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    ResidentId = id,
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = TrimOrNull(m.Phone),
                    IdentityNumber = TrimOrNull(m.IdentityNumber),
                    Relationship = TrimOrNull(m.Relationship)
                });
            }

            resident.UpdatedAtUtc = DateTime.UtcNow;
            resident.UpdatedByUserId = _currentUserService.UserId;

            // CRITICAL FIX: Clear the tracker and re-attach only the resident to avoid navigation property conflicts
            // This stops the Audit Logger from trying to update related entities (Site, Unit) which causes the 0 rows affected error.
            var context = ((dynamic)_residentRepository).DbContext;
            context.ChangeTracker.Clear();
            context.Set<SiteResident>().Attach(resident);
            context.Entry(resident).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            
            // Also ensure household members are marked as Added (since we cleared the tracker)
            foreach (var member in resident.HouseholdMembers)
            {
                context.Entry(member).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }

            await _residentRepository.SaveChangesAsync(cancellationToken);

            // Fetch fresh copy to return
            var freshResident = await GetTrackedResidentAsync(id, asNoTracking: true, cancellationToken);
            return DataResponse<ResidentDetailDto>.Succeed(MapDetail(freshResident), _localizer["ResidentUpdatedSuccessfully"].Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR IN UpdateAsync: {ex}");
            return DataResponse<ResidentDetailDto>.Fail($"DEBUG ERROR: {ex.Message}");
        }
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
        return Response.Succeed(_localizer["ResidentArchivedSuccessfully"].Value);
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
            throw new KeyNotFoundException(_localizer["UnitNotFoundInSite"].Value);
        }
    }

    private async Task EnsureUnitIsAvailableAsync(Guid? unitId, Guid? currentResidentId, CancellationToken cancellationToken)
    {
        if (!unitId.HasValue) return;

        var activeResidentQuery = _residentRepository.Query()
            .Where(r => r.UnitId == unitId.Value && !r.IsArchived);

        if (currentResidentId.HasValue)
        {
            activeResidentQuery = activeResidentQuery.Where(r => r.Id != currentResidentId.Value);
        }

        var isOccupied = await activeResidentQuery
            .AnyAsync(r => r.IsActive || r.MoveOutDate == null, cancellationToken);

        if (isOccupied)
        {
            var message = _localizer["ActiveResidentExistsInUnit"].Value;
            if (string.IsNullOrWhiteSpace(message) || message == "ActiveResidentExistsInUnit")
            {
                message = "Bu dairede halihazırda aktif bir kayıt bulunmaktadır. Yeni kişi kaydetmek için öncelikle mevcut kişinin durumunu pasif yapmalı ve çıkış tarihini girmelisiniz.";
            }
            throw new BadHttpRequestException(message);
        }
    }

    private async Task<SiteResident> GetTrackedResidentAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var resident = await _residentRepository.Query(asNoTracking)
            .Include(entity => entity.Site)
            .Include(entity => entity.Unit)
                .ThenInclude(unit => unit!.SiteBlock)
            .Include(entity => entity.HouseholdMembers)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return resident ?? throw new KeyNotFoundException(_localizer["ResidentNotFound"].Value);
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
        KvkkConsentGiven = resident.KvkkConsentGiven,
        CommunicationConsentGiven = resident.CommunicationConsentGiven,
        Notes = resident.Notes,
        CreatedAtUtc = resident.CreatedAtUtc,
        ArchivedAtUtc = resident.ArchivedAtUtc,
        CreatedByUserId = resident.CreatedByUserId,
        UpdatedByUserId = resident.UpdatedByUserId,
        OwnerFirstName = resident.OwnerFirstName,
        OwnerLastName = resident.OwnerLastName,
        OwnerPhone = resident.OwnerPhone,
        HouseholdMembers = resident.HouseholdMembers.Select(m => new HouseholdMemberDto
        {
            Id = m.Id,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Phone = m.Phone,
            IdentityNumber = m.IdentityNumber,
            Relationship = m.Relationship
        }).ToList()
    };

    private static string GetFullName(SiteResident resident) => $"{resident.FirstName} {resident.LastName}".Trim();
    private static string? NormalizeSearchTerm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
