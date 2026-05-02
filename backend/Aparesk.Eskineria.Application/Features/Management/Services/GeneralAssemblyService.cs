using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Domain.Enums;
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
                    .ThenInclude(r => r.Unit)
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
                IsActive = bm.IsActive,
                ResidentName = bm.Resident != null ? $"{bm.Resident.FirstName} {bm.Resident.LastName}" : "Bilinmeyen Sakin",
                UnitNumber = bm.Resident?.Unit?.Number
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
                IsActive = bm.IsActive,
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
        var assembly = await _assemblyRepository.Query()
            .Where(x => x.Id == id)
            .Select(x => new { x.SiteId })
            .FirstOrDefaultAsync(cancellationToken);

        if (assembly == null)
            throw new KeyNotFoundException("General assembly not found.");

        var agendaItems = request.AgendaItems ?? new();
        var decisions = request.Decisions ?? new();
        var boardMembers = request.BoardMembers ?? new();

        await EnsureBoardMembersBelongToSiteAsync(assembly.SiteId, boardMembers, cancellationToken);

        var context = GetDbContext();
        var managementBoardUpdateMode = await EnsureManagementBoardUpdateIsAllowedAsync(context, id, boardMembers, cancellationToken);
        var shouldPreserveManagementBoard = managementBoardUpdateMode == ManagementBoardUpdateMode.LockedUnchanged;
        var boardMembersToPersist = (shouldPreserveManagementBoard
                ? boardMembers.Where(x => x.BoardType != BoardType.ManagementBoard)
                : boardMembers)
            .ToList();

        var now = DateTime.UtcNow;
        var currentUserId = _currentUserService.UserId;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Set<GeneralAssembly>()
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.MeetingDate, request.MeetingDate)
                .SetProperty(x => x.SecondMeetingDate, request.SecondMeetingDate)
                .SetProperty(x => x.Term, request.Term)
                .SetProperty(x => x.Location, request.Location)
                .SetProperty(x => x.Type, request.Type)
                .SetProperty(x => x.IsCompleted, request.IsCompleted)
                .SetProperty(x => x.UpdatedAtUtc, now)
                .SetProperty(x => x.UpdatedByUserId, currentUserId),
                cancellationToken);

        await context.Set<GeneralAssemblyAgendaItem>()
            .Where(x => x.GeneralAssemblyId == id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<GeneralAssemblyDecision>()
            .Where(x => x.GeneralAssemblyId == id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<BoardMember>()
            .Where(x => x.GeneralAssemblyId == id)
            .Where(x => !shouldPreserveManagementBoard || x.BoardType != BoardType.ManagementBoard)
            .ExecuteDeleteAsync(cancellationToken);

        if (agendaItems.Count > 0)
        {
            await context.Set<GeneralAssemblyAgendaItem>().AddRangeAsync(agendaItems.Select(a => new GeneralAssemblyAgendaItem
            {
                Id = Guid.NewGuid(),
                GeneralAssemblyId = id,
                Order = a.Order,
                Description = a.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = currentUserId,
                UpdatedByUserId = currentUserId
            }), cancellationToken);
        }

        if (decisions.Count > 0)
        {
            await context.Set<GeneralAssemblyDecision>().AddRangeAsync(decisions.Select(d => new GeneralAssemblyDecision
            {
                Id = Guid.NewGuid(),
                GeneralAssemblyId = id,
                DecisionNumber = d.DecisionNumber,
                Description = d.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = currentUserId,
                UpdatedByUserId = currentUserId
            }), cancellationToken);
        }

        if (boardMembersToPersist.Count > 0)
        {
            await context.Set<BoardMember>().AddRangeAsync(boardMembersToPersist.Select(bm => new BoardMember
            {
                Id = Guid.NewGuid(),
                GeneralAssemblyId = id,
                ResidentId = bm.ResidentId,
                BoardType = bm.BoardType,
                MemberType = bm.MemberType,
                Title = bm.Title,
                IsActive = bm.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = currentUserId,
                UpdatedByUserId = currentUserId
            }), cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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

    private async Task EnsureBoardMembersBelongToSiteAsync(
        Guid siteId,
        IReadOnlyCollection<GeneralAssemblyBoardMemberDto> boardMembers,
        CancellationToken cancellationToken)
    {
        if (boardMembers.Any(x => x.ResidentId == Guid.Empty))
        {
            throw new ArgumentException("Kurul üyeleri için kişi seçilmelidir.");
        }

        var residentIds = boardMembers
            .Select(x => x.ResidentId)
            .Distinct()
            .ToArray();

        if (residentIds.Length == 0)
        {
            return;
        }

        var validResidentCount = await _residentRepository.Query()
            .CountAsync(x => residentIds.Contains(x.Id) && x.SiteId == siteId && !x.IsArchived, cancellationToken);

        if (validResidentCount != residentIds.Length)
        {
            throw new ArgumentException("Seçilen kurul üyelerinden bazıları bu siteye ait değil veya geçerli değil.");
        }
    }

    private static async Task<ManagementBoardUpdateMode> EnsureManagementBoardUpdateIsAllowedAsync(
        DbContext context,
        Guid generalAssemblyId,
        IReadOnlyCollection<GeneralAssemblyBoardMemberDto> incomingBoardMembers,
        CancellationToken cancellationToken)
    {
        var existingRows = await context.Set<BoardMember>()
            .AsNoTracking()
            .Where(x => x.GeneralAssemblyId == generalAssemblyId && x.BoardType == BoardType.ManagementBoard)
            .Select(x => new { x.ResidentId, x.MemberType, x.Title, x.IsActive })
            .ToListAsync(cancellationToken);

        if (existingRows.Count == 0)
        {
            return ManagementBoardUpdateMode.NotLocked;
        }

        var existingSignatures = NormalizeManagementBoardSignatures(existingRows.Select(x =>
            new BoardMemberSignature(x.ResidentId, x.MemberType, NormalizeTitle(x.Title), x.IsActive)));

        var incomingSignatures = NormalizeManagementBoardSignatures(incomingBoardMembers
            .Where(x => x.BoardType == BoardType.ManagementBoard)
            .Select(x => new BoardMemberSignature(x.ResidentId, x.MemberType, NormalizeTitle(x.Title), x.IsActive)));

        if (existingSignatures.SequenceEqual(incomingSignatures))
        {
            return ManagementBoardUpdateMode.LockedUnchanged;
        }

        if (IsSingleSubstitutePromotion(existingSignatures, incomingSignatures))
        {
            return ManagementBoardUpdateMode.SubstitutePromotion;
        }

        throw new ArgumentException("Yönetim kurulu onaylandıktan sonra yalnızca asil üye istifasında bir yedek üye asil olarak atanabilir.");
    }

    private static BoardMemberSignature[] NormalizeManagementBoardSignatures(IEnumerable<BoardMemberSignature> signatures)
    {
        return signatures
            .OrderBy(x => x.ResidentId)
            .ThenBy(x => (int)x.MemberType)
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsSingleSubstitutePromotion(
        IReadOnlyCollection<BoardMemberSignature> existingSignatures,
        IReadOnlyCollection<BoardMemberSignature> incomingSignatures)
    {
        if (incomingSignatures.Count != existingSignatures.Count)
        {
            return false;
        }

        var existingByResident = existingSignatures
            .GroupBy(x => x.ResidentId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var incomingByResident = incomingSignatures
            .GroupBy(x => x.ResidentId)
            .ToDictionary(x => x.Key, x => x.ToList());

        if (existingByResident.Any(x => x.Value.Count != 1) || incomingByResident.Any(x => x.Value.Count != 1))
        {
            return false;
        }

        if (incomingByResident.Keys.Any(x => !existingByResident.ContainsKey(x)))
        {
            return false;
        }

        var changedMembers = existingByResident
            .Where(x => incomingByResident.ContainsKey(x.Key))
            .Select(x => new
            {
                Existing = x.Value[0],
                Incoming = incomingByResident[x.Key][0]
            })
            .Where(x => x.Existing != x.Incoming)
            .ToArray();

        if (changedMembers.Length != 2)
        {
            return false;
        }

        var resignedMember = changedMembers.FirstOrDefault(x => x.Existing.IsActive && !x.Incoming.IsActive);
        var promotedMember = changedMembers.FirstOrDefault(x => x.Existing.MemberType == BoardMemberType.Substitute && x.Incoming.MemberType == BoardMemberType.Principal);

        if (resignedMember == null || promotedMember == null)
        {
            return false;
        }

        if (resignedMember.Existing.MemberType != BoardMemberType.Principal)
        {
            return false;
        }

        return !promotedMember.Incoming.Title.Equals("Yedek Üye", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTitle(string? title)
    {
        return (title ?? string.Empty).Trim();
    }

    private DbContext GetDbContext()
    {
        return ((dynamic)_assemblyRepository).DbContext;
    }

    private sealed record BoardMemberSignature(Guid ResidentId, BoardMemberType MemberType, string Title, bool IsActive);

    private enum ManagementBoardUpdateMode
    {
        NotLocked,
        LockedUnchanged,
        SubstitutePromotion
    }
}
