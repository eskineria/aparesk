using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Persistence.Features.Management.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Application.Features.Management.Services;

public sealed class GeneralAssemblyService : IGeneralAssemblyService
{
    private readonly IGeneralAssemblyRepository _assemblyRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly ISiteResidentRepository _residentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GeneralAssemblyService(
        IGeneralAssemblyRepository assemblyRepository,
        ISiteRepository siteRepository,
        ISiteResidentRepository residentRepository,
        ICurrentUserService currentUserService)
    {
        _assemblyRepository = assemblyRepository;
        _siteRepository = siteRepository;
        _residentRepository = residentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<GeneralAssemblyListItemDto>> GetPagedAsync(GetGeneralAssembliesRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _assemblyRepository.Query()
            .Include(x => x.Site)
            .Where(x =>
                (!request.SiteId.HasValue || x.SiteId == request.SiteId.Value) &&
                (!request.IncludeCompleted.HasValue || x.IsCompleted == request.IncludeCompleted.Value) &&
                (string.IsNullOrWhiteSpace(request.SearchTerm) || x.Term.Contains(request.SearchTerm)));

        var page = await query
            .OrderByDescending(x => x.MeetingDate)
            .ToPaginateAsync(pageNumber - 1, pageSize, cancellationToken);

        var items = page.Items.Select(x => new GeneralAssemblyListItemDto
        {
            Id = x.Id,
            SiteId = x.SiteId,
            SiteName = x.Site.Name,
            MeetingDate = x.MeetingDate,
            SecondMeetingDate = x.SecondMeetingDate,
            Term = x.Term,
            Location = x.Location,
            Type = x.Type,
            IsCompleted = x.IsCompleted,
            UpdatedAtUtc = x.UpdatedAtUtc ?? x.CreatedAtUtc
        }).ToList();

        return new PagedResponse<GeneralAssemblyListItemDto>(items, page.Index, page.Size, page.Count, page.Pages, page.HasPrevious, page.HasNext);
    }

    public async Task<DataResponse<GeneralAssemblyDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _assemblyRepository.Query()
            .Include(x => x.Site)
            .Include(x => x.AgendaItems)
            .Include(x => x.Decisions)
            .Include(x => x.BoardMembers)
                .ThenInclude(bm => bm.Resident)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            throw new KeyNotFoundException("General assembly not found.");

        var dto = new GeneralAssemblyDetailDto
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            SiteName = entity.Site.Name,
            MeetingDate = entity.MeetingDate,
            SecondMeetingDate = entity.SecondMeetingDate,
            Term = entity.Term,
            Location = entity.Location,
            Type = entity.Type,
            IsCompleted = entity.IsCompleted,
            UpdatedAtUtc = entity.UpdatedAtUtc ?? entity.CreatedAtUtc,
            AgendaItems = entity.AgendaItems.OrderBy(a => a.Order).Select(a => new GeneralAssemblyAgendaItemDto
            {
                Order = a.Order,
                Description = a.Description
            }).ToList(),
            Decisions = entity.Decisions.Select(d => new GeneralAssemblyDecisionDto
            {
                Id = d.Id,
                DecisionNumber = d.DecisionNumber,
                Description = d.Description
            }).ToList(),
            BoardMembers = entity.BoardMembers.Select(bm => new GeneralAssemblyBoardMemberResponseDto
            {
                Id = bm.Id,
                ResidentId = bm.ResidentId,
                BoardType = bm.BoardType,
                MemberType = bm.MemberType,
                Title = bm.Title,
                ResidentName = $"{bm.Resident.FirstName} {bm.Resident.LastName}",
                UnitNumber = bm.Resident.UnitId.HasValue ? GetUnitNumber(bm.Resident.UnitId.Value) : null
            }).ToList()
        };

        return DataResponse<GeneralAssemblyDetailDto>.Succeed(dto);
    }

    public async Task<DataResponse<GeneralAssemblyDetailDto>> CreateAsync(CreateGeneralAssemblyRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = new GeneralAssembly
        {
            Id = Guid.NewGuid(),
            SiteId = request.SiteId,
            MeetingDate = request.MeetingDate,
            SecondMeetingDate = request.SecondMeetingDate,
            Term = request.Term,
            Location = request.Location,
            Type = request.Type,
            IsCompleted = request.IsCompleted,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = _currentUserService.UserId,
            UpdatedByUserId = _currentUserService.UserId,
            AgendaItems = request.AgendaItems.Select(a => new GeneralAssemblyAgendaItem
            {
                Id = Guid.NewGuid(),
                Order = a.Order,
                Description = a.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = _currentUserService.UserId,
                UpdatedByUserId = _currentUserService.UserId
            }).ToList(),
            Decisions = request.Decisions.Select(d => new GeneralAssemblyDecision
            {
                Id = Guid.NewGuid(),
                DecisionNumber = d.DecisionNumber,
                Description = d.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = _currentUserService.UserId,
                UpdatedByUserId = _currentUserService.UserId
            }).ToList(),
            BoardMembers = request.BoardMembers.Select(bm => new BoardMember
            {
                Id = Guid.NewGuid(),
                ResidentId = bm.ResidentId,
                BoardType = bm.BoardType,
                MemberType = bm.MemberType,
                Title = bm.Title,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = _currentUserService.UserId,
                UpdatedByUserId = _currentUserService.UserId
            }).ToList()
        };

        await _assemblyRepository.AddAsync(entity, cancellationToken);
        await _assemblyRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<DataResponse<GeneralAssemblyDetailDto>> UpdateAsync(Guid id, UpdateGeneralAssemblyRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Load the main entity WITH tracking (No includes needed, ExecuteDelete handles cleanup)
        var entity = await _assemblyRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            
        if (entity == null)
            throw new KeyNotFoundException("General assembly not found.");

        var context = (DbContext)((dynamic)_assemblyRepository).DbContext;
        var now = DateTime.UtcNow;

        // 2. Clean old items directly in DB (Fast and clean)
        await context.Set<GeneralAssemblyAgendaItem>().Where(x => x.GeneralAssemblyId == id).ExecuteDeleteAsync(cancellationToken);
        await context.Set<GeneralAssemblyDecision>().Where(x => x.GeneralAssemblyId == id).ExecuteDeleteAsync(cancellationToken);
        await context.Set<BoardMember>().Where(x => x.GeneralAssemblyId == id).ExecuteDeleteAsync(cancellationToken);

        // 3. Update main entity properties
        entity.MeetingDate = request.MeetingDate;
        entity.SecondMeetingDate = request.SecondMeetingDate;
        entity.Term = request.Term;
        entity.Location = request.Location;
        entity.Type = request.Type;
        entity.IsCompleted = request.IsCompleted;
        entity.UpdatedAtUtc = now;
        entity.UpdatedByUserId = _currentUserService.UserId;

        // 4. Clear internal collections in memory (to avoid tracking old deleted items)
        entity.AgendaItems.Clear();
        entity.Decisions.Clear();
        entity.BoardMembers.Clear();

        // 5. Add NEW items to the collections (Since entity is tracked, EF will save these as new)
        foreach (var a in request.AgendaItems)
        {
            entity.AgendaItems.Add(new GeneralAssemblyAgendaItem
            {
                Id = Guid.NewGuid(),
                GeneralAssemblyId = id,
                Order = a.Order,
                Description = a.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = _currentUserService.UserId,
                UpdatedByUserId = _currentUserService.UserId
            });
        }

        foreach (var d in request.Decisions)
        {
            entity.Decisions.Add(new GeneralAssemblyDecision
            {
                Id = Guid.NewGuid(),
                GeneralAssemblyId = id,
                DecisionNumber = d.DecisionNumber,
                Description = d.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = _currentUserService.UserId,
                UpdatedByUserId = _currentUserService.UserId
            });
        }

        foreach (var bm in request.BoardMembers)
        {
            entity.BoardMembers.Add(new BoardMember
            {
                Id = Guid.NewGuid(),
                GeneralAssemblyId = id,
                ResidentId = bm.ResidentId,
                BoardType = bm.BoardType,
                MemberType = bm.MemberType,
                Title = bm.Title,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = _currentUserService.UserId,
                UpdatedByUserId = _currentUserService.UserId
            });
        }

        // 6. Atomic Save (EF will handle the new collection items automatically)
        await _assemblyRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Response> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _assemblyRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("General assembly not found.");

        await _assemblyRepository.DeleteAsync(entity, cancellationToken);
        await _assemblyRepository.SaveChangesAsync(cancellationToken);

        return Response.Succeed("General assembly deleted successfully.");
    }

    private string? GetUnitNumber(Guid unitId)
    {
        // Simple helper to avoid complex includes for now
        var context = ((dynamic)_assemblyRepository).DbContext;
        var unit = (context as DbContext).Set<Unit>().Find(unitId);
        return unit?.Number;
    }
}
