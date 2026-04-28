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

public sealed class SiteService : ISiteService
{
    private readonly ISiteRepository _siteRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SiteService> _localizer;

    public SiteService(ISiteRepository siteRepository, ICurrentUserService currentUserService, IStringLocalizer<SiteService> localizer)
    {
        _siteRepository = siteRepository;
        _currentUserService = currentUserService;
        _localizer = localizer;
    }

    public async Task<PagedResponse<SiteListItemDto>> GetPagedAsync(GetSitesRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var searchTerm = NormalizeSearchTerm(request.SearchTerm);

        var query = _siteRepository.Query()
            .Include(site => site.Blocks)
            .Include(site => site.Units)
            .Where(site =>
                (request.IncludeArchived || !site.IsArchived) &&
                (!request.IsActive.HasValue || site.IsActive == request.IsActive.Value) &&
                (searchTerm == null ||
                    site.Name.Contains(searchTerm) ||
                    (site.City != null && site.City.Contains(searchTerm)) ||
                    (site.District != null && site.District.Contains(searchTerm))));

        var page = await query
            .OrderBy(site => site.Name)
            .ToPaginateAsync(pageNumber - 1, pageSize, cancellationToken);

        var items = page.Items.Select(MapListItem).ToList();
        return new PagedResponse<SiteListItemDto>(items, page.Index, page.Size, page.Count, page.Pages, page.HasPrevious, page.HasNext);
    }

    public async Task<DataResponse<SiteDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var site = await GetTrackedSiteAsync(id, asNoTracking: true, cancellationToken);
        return DataResponse<SiteDetailDto>.Succeed(MapDetail(site));
    }

    public async Task<DataResponse<SiteDetailDto>> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var site = new Site
        {
            Id = Guid.NewGuid(),
            Code = await GenerateUniqueCodeAsync(request.Name, cancellationToken),
            Name = request.Name.Trim(),
            TaxNumber = TrimOrNull(request.TaxNumber),
            TaxOffice = TrimOrNull(request.TaxOffice),
            Phone = TrimOrNull(request.Phone),
            Email = TrimOrNull(request.Email),
            AddressLine = TrimOrNull(request.AddressLine),
            District = TrimOrNull(request.District),
            City = TrimOrNull(request.City),
            PostalCode = TrimOrNull(request.PostalCode),
            IsActive = request.IsActive,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = _currentUserService.UserId,
            UpdatedByUserId = _currentUserService.UserId
        };

        await _siteRepository.AddAsync(site, cancellationToken);
        await _siteRepository.SaveChangesAsync(cancellationToken);
        return DataResponse<SiteDetailDto>.Succeed(MapDetail(site), _localizer["SiteCreatedSuccessfully"].Value, StatusCodes.Status201Created);
    }

    public async Task<DataResponse<SiteDetailDto>> UpdateAsync(Guid id, UpdateSiteRequest request, CancellationToken cancellationToken = default)
    {
        var site = await GetTrackedSiteAsync(id, asNoTracking: false, cancellationToken);

        site.Name = request.Name.Trim();
        site.TaxNumber = TrimOrNull(request.TaxNumber);
        site.TaxOffice = TrimOrNull(request.TaxOffice);
        site.Phone = TrimOrNull(request.Phone);
        site.Email = TrimOrNull(request.Email);
        site.AddressLine = TrimOrNull(request.AddressLine);
        site.District = TrimOrNull(request.District);
        site.City = TrimOrNull(request.City);
        site.PostalCode = TrimOrNull(request.PostalCode);
        site.IsActive = request.IsActive;
        site.UpdatedAtUtc = DateTime.UtcNow;
        site.UpdatedByUserId = _currentUserService.UserId;

        await _siteRepository.SaveChangesAsync(cancellationToken);
        return DataResponse<SiteDetailDto>.Succeed(MapDetail(site), _localizer["SiteUpdatedSuccessfully"].Value);
    }

    public async Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var site = await GetTrackedSiteAsync(id, asNoTracking: false, cancellationToken);
        site.IsArchived = true;
        site.IsActive = false;
        site.ArchivedAtUtc = DateTime.UtcNow;
        site.UpdatedAtUtc = site.ArchivedAtUtc.Value;
        site.UpdatedByUserId = _currentUserService.UserId;

        await _siteRepository.SaveChangesAsync(cancellationToken);
        return Response.Succeed(_localizer["SiteArchivedSuccessfully"].Value);
    }

    private async Task<Site> GetTrackedSiteAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.Query(asNoTracking)
            .Include(entity => entity.Blocks)
            .Include(entity => entity.Units)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return site ?? throw new KeyNotFoundException(_localizer["SiteNotFound"].Value);
    }

    private async Task<string> GenerateUniqueCodeAsync(string name, CancellationToken cancellationToken)
    {
        var baseCode = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (baseCode.Length > 10) baseCode = baseCode[..10];
        if (string.IsNullOrEmpty(baseCode)) baseCode = "S";

        var code = baseCode;
        int counter = 1;
        while (await _siteRepository.Query().AnyAsync(s => s.Code == code, cancellationToken))
        {
            code = $"{baseCode}{counter++}";
        }
        return code;
    }

    private static SiteListItemDto MapListItem(Site site) => new()
    {
        Id = site.Id,
        Name = site.Name,
        City = site.City,
        District = site.District,
        IsActive = site.IsActive,
        IsArchived = site.IsArchived,
        BlockCount = site.Blocks.Count(block => !block.IsArchived),
        UnitCount = site.Units.Count(unit => !unit.IsArchived),
        UpdatedAtUtc = site.UpdatedAtUtc
    };

    private static SiteDetailDto MapDetail(Site site) => new()
    {
        Id = site.Id,
        Name = site.Name,
        City = site.City,
        District = site.District,
        IsActive = site.IsActive,
        IsArchived = site.IsArchived,
        BlockCount = site.Blocks.Count(block => !block.IsArchived),
        UnitCount = site.Units.Count(unit => !unit.IsArchived),
        UpdatedAtUtc = site.UpdatedAtUtc,
        TaxNumber = site.TaxNumber,
        TaxOffice = site.TaxOffice,
        Phone = site.Phone,
        Email = site.Email,
        AddressLine = site.AddressLine,
        PostalCode = site.PostalCode,
        CreatedAtUtc = site.CreatedAtUtc,
        ArchivedAtUtc = site.ArchivedAtUtc,
        CreatedByUserId = site.CreatedByUserId,
        UpdatedByUserId = site.UpdatedByUserId
    };

    private static string? NormalizeSearchTerm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
