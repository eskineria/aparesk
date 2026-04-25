using Eskineria.Core.Shared.Response;
using Microsoft.Extensions.Localization;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Eskineria.Core.Compliance.Abstractions;
using Eskineria.Core.Compliance.Models;
using Eskineria.Core.Auth.Entities;
using Eskineria.Core.Shared.Exceptions;
using Eskineria.Core.Compliance.Entities;
using Eskineria.Core.Repository.Specification;
using Eskineria.Core.Notifications.Abstractions;
using Eskineria.Core.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MapsterMapper;

namespace Eskineria.Core.Compliance.Services;

public class ComplianceService : IComplianceService
{
    private static readonly string[] RequiredAcceptanceTypes = ["TermsOfService", "PrivacyPolicy"];

    private readonly ITermsRepository _termsRepository;
    private readonly IUserTermsAcceptanceRepository _userTermsAcceptanceRepository;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ComplianceService> _localizer;
    private readonly IAuditingStore _auditingStore;
    private readonly UserManager<EskineriaUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public ComplianceService(
        ITermsRepository termsRepository,
        IUserTermsAcceptanceRepository userTermsAcceptanceRepository,
        IMapper mapper,
        IStringLocalizer<ComplianceService> localizer,
        IAuditingStore auditingStore,
        UserManager<EskineriaUser> userManager,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _termsRepository = termsRepository;
        _userTermsAcceptanceRepository = userTermsAcceptanceRepository;
        _mapper = mapper;
        _localizer = localizer;
        _auditingStore = auditingStore;
        _userManager = userManager;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    public async Task<DataResponse<List<TermsDto>>> GetAllTermsAsync(string? type = null)
    {
        var normalizedType = NormalizeTypeOrNull(type);
        var spec = new QuerySpecification<TermsAndConditions>(x =>
                normalizedType == null || x.Type == normalizedType)
            .OrderByDescending(x => x.EffectiveDate);

        var terms = await _termsRepository.GetListAsync(spec);

        var dtos = _mapper.Map<List<TermsDto>>(terms);
        return DataResponse<List<TermsDto>>.Succeed(dtos, _localizer[LocalizationKeys.TermsRetrievedSuccessfully]);
    }

    public async Task<DataResponse<TermsDto>> GetActiveTermsByTypeAsync(string type)
    {
        var normalizedType = NormalizeTypeOrNull(type);
        if (normalizedType == null)
        {
            return DataResponse<TermsDto>.Fail("Terms type is required.");
        }

        var terms = await GetLatestActiveTermByTypeAsync(normalizedType);

        if (terms == null)
            return DataResponse<TermsDto>.Fail(_localizer[LocalizationKeys.NoActiveTermsFoundForType, normalizedType], 404);

        var dto = _mapper.Map<TermsDto>(terms);
        return DataResponse<TermsDto>.Succeed(dto, _localizer[LocalizationKeys.ActiveTermsRetrievedSuccessfully]);
    }

    public async Task<DataResponse<TermsDto>> GetTermsByIdAsync(Guid id)
    {
        var terms = await _termsRepository.GetByIdAsync(id);
        if (terms == null)
            return DataResponse<TermsDto>.Fail(_localizer[LocalizationKeys.TermsNotFound], 404);

        var dto = _mapper.Map<TermsDto>(terms);
        return DataResponse<TermsDto>.Succeed(dto, _localizer[LocalizationKeys.TermsRetrievedSuccessfully]);
    }

    public async Task<DataResponse<TermsDto>> CreateTermsAsync(CreateTermsDto dto)
    {
        TermsAndConditions terms;
        try
        {
            var normalizedType = NormalizeRequiredType(dto.Type);
            var normalizedVersion = dto.Version.Trim();

            var existingByTypeAndVersion = await _termsRepository.GetAsync(
                x => x.Type == normalizedType && x.Version == normalizedVersion);
            if (existingByTypeAndVersion != null)
            {
                return DataResponse<TermsDto>.Fail("A terms document with the same type and version already exists.");
            }

            terms = TermsAndConditions.Create(
                normalizedType,
                normalizedVersion,
                dto.Content,
                dto.Summary,
                dto.EffectiveDate);
        }
        catch (DomainException ex)
        {
            return DataResponse<TermsDto>.Fail(ex.Message);
        }

        await _termsRepository.AddAsync(terms);
        await _termsRepository.SaveChangesAsync();

        var createdDto = _mapper.Map<TermsDto>(terms);
        return DataResponse<TermsDto>.Succeed(createdDto, _localizer[LocalizationKeys.TermsCreatedSuccessfully], 201);
    }

    public async Task<Response> UpdateTermsAsync(Guid id, UpdateTermsDto dto)
    {
        var terms = await _termsRepository.GetByIdAsync(id);
        if (terms == null)
            return Response.Fail(_localizer[LocalizationKeys.TermsNotFound], 404);

        var activatingViaUpdate = !terms.IsActive && dto.IsActive;
        if (activatingViaUpdate)
        {
            await DeactivateOtherActiveTermsOfSameTypeAsync(terms.Type, terms.Id);
        }

        try
        {
            terms.UpdateContent(dto.Content, dto.Summary, dto.IsActive);
        }
        catch (DomainException ex)
        {
            return Response.Fail(ex.Message);
        }

        await _termsRepository.UpdateAsync(terms);
        await _termsRepository.SaveChangesAsync();

        return Response.Succeed(_localizer[LocalizationKeys.TermsUpdatedSuccessfully]);
    }

    public async Task<Response> DeleteTermsAsync(Guid id)
    {
        var terms = await _termsRepository.GetByIdAsync(id);
        if (terms == null)
            return Response.Fail(_localizer[LocalizationKeys.TermsNotFound], 404);

        // Check if there are any acceptances
        var hasAcceptances = await _userTermsAcceptanceRepository.GetAsync(x => x.TermsAndConditionsId == id) != null;
        if (hasAcceptances)
        {
            return Response.Fail(_localizer["TermsCannotBeDeletedWithAcceptances"]);
        }

        await _termsRepository.DeleteAsync(terms);
        await _termsRepository.SaveChangesAsync();

        return Response.Succeed(_localizer[LocalizationKeys.TermsDeletedSuccessfully]);
    }

    public async Task<Response> ActivateTermsAsync(Guid id)
    {
        var terms = await _termsRepository.GetByIdAsync(id);
        if (terms == null)
            return Response.Fail(_localizer[LocalizationKeys.TermsNotFound], 404);

        var wasActive = terms.IsActive;

        await DeactivateOtherActiveTermsOfSameTypeAsync(terms.Type, terms.Id);

        // Activate this one
        terms.Activate();
        await _termsRepository.SaveChangesAsync();

        if (!wasActive && IsRequiredAcceptanceType(terms.Type))
        {
            await NotifyUsersAboutUpdatedTermsAsync(terms);
        }

        return Response.Succeed(_localizer[LocalizationKeys.TermsActivatedSuccessfully]);
    }

    public async Task<Response> AcceptTermsAsync(Guid userId, Guid termsAndConditionsId, string? ipAddress, string? userAgent)
    {
        var terms = await _termsRepository.GetByIdAsync(termsAndConditionsId);
        if (terms == null)
        {
            return Response.Fail(_localizer[LocalizationKeys.TermsNotFound], 404);
        }

        if (!terms.IsActive)
        {
            return Response.Fail("Only active terms can be accepted.");
        }

        if (terms.EffectiveDate > DateTime.UtcNow)
        {
            return Response.Fail("This terms version is not effective yet.");
        }

        // Check if already accepted
        var existing = await _userTermsAcceptanceRepository.GetAsync(
            x => x.UserId == userId && x.TermsAndConditionsId == termsAndConditionsId);
        if (existing != null)
            return Response.Succeed(_localizer[LocalizationKeys.TermsAlreadyAccepted]);

        UserTermsAcceptance acceptance;
        try
        {
            acceptance = UserTermsAcceptance.Create(
                userId,
                termsAndConditionsId,
                ipAddress,
                userAgent);
        }
        catch (DomainException ex)
        {
            return Response.Fail(ex.Message);
        }

        await _userTermsAcceptanceRepository.AddAsync(acceptance);
        await _userTermsAcceptanceRepository.SaveChangesAsync();

        await _auditingStore.SaveAsync(new AuditLog
        {
            ServiceName = nameof(ComplianceService),
            MethodName = nameof(AcceptTermsAsync),
            Parameters = JsonSerializer.Serialize(new
            {
                Before = (object?)null,
                After = new
                {
                    UserId = userId,
                    TermsId = termsAndConditionsId,
                    TermsType = terms.Type,
                    TermsVersion = terms.Version,
                    AcceptedAt = acceptance.AcceptedAt
                }
            })
        });

        return Response.Succeed(_localizer[LocalizationKeys.TermsAcceptedSuccessfully], 201);
    }

    public async Task<DataResponse<List<UserTermsAcceptanceDto>>> GetUserAcceptancesAsync(Guid userId)
    {
        var acceptances = await _userTermsAcceptanceRepository.GetListAsync(
            new QuerySpecification<UserTermsAcceptance>(x => x.UserId == userId)
                .Include(x => x.TermsAndConditions!)
                .OrderByDescending(x => x.AcceptedAt));
        var dtos = _mapper.Map<List<UserTermsAcceptanceDto>>(acceptances);
        return DataResponse<List<UserTermsAcceptanceDto>>.Succeed(dtos, _localizer[LocalizationKeys.UserAcceptancesRetrievedSuccessfully]);
    }

    public async Task<DataResponse<bool>> HasUserAcceptedLatestTermsAsync(Guid userId, string type)
    {
        var normalizedType = NormalizeTypeOrNull(type);
        if (normalizedType == null)
        {
            return DataResponse<bool>.Fail("Terms type is required.");
        }

        var activeTerms = await GetLatestActiveTermByTypeAsync(normalizedType);
        if (activeTerms == null)
            return DataResponse<bool>.Succeed(true, _localizer[LocalizationKeys.NoActiveTermsFound]); // No terms to accept

        var hasAccepted = await _userTermsAcceptanceRepository.GetAsync(
            x => x.UserId == userId && x.TermsAndConditionsId == activeTerms.Id) != null;
        return DataResponse<bool>.Succeed(
            hasAccepted,
            hasAccepted
                ? _localizer[LocalizationKeys.UserHasAcceptedLatestTerms]
                : _localizer[LocalizationKeys.UserHasNotAcceptedLatestTerms]);
    }

    public async Task<DataResponse<List<TermsDto>>> GetPendingRequiredTermsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var requiredTypes = RequiredAcceptanceTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeRequiredTerms = await _termsRepository.GetListAsync(
            new QuerySpecification<TermsAndConditions>(x =>
                x.IsActive &&
                x.EffectiveDate <= now &&
                requiredTypes.Contains(x.Type))
                .OrderByDescending(x => x.EffectiveDate));

        // Group by type and select latest
        var latestByType = activeRequiredTerms
            .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.EffectiveDate).First())
            .ToList();

        if (latestByType.Count == 0)
        {
            return DataResponse<List<TermsDto>>.Succeed(
                [],
                _localizer["PendingRequiredTermsRetrievedSuccessfully"]);
        }

        var latestTermIds = latestByType.Select(x => x.Id).ToHashSet();
        var acceptedForLatest = await _userTermsAcceptanceRepository.GetListAsync(
            new QuerySpecification<UserTermsAcceptance>(x =>
                x.UserId == userId &&
                latestTermIds.Contains(x.TermsAndConditionsId)));

        var acceptedTermIds = acceptedForLatest
            .Select(x => x.TermsAndConditionsId)
            .ToHashSet();

        var pendingTerms = latestByType
            .Where(x => !acceptedTermIds.Contains(x.Id))
            .ToList();

        var dtos = _mapper.Map<List<TermsDto>>(pendingTerms);
        return DataResponse<List<TermsDto>>.Succeed(
            dtos,
            _localizer["PendingRequiredTermsRetrievedSuccessfully"]);
    }

    private static bool IsRequiredAcceptanceType(string type)
    {
        return RequiredAcceptanceTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    private async Task DeactivateOtherActiveTermsOfSameTypeAsync(string type, Guid activeTermsId)
    {
        var otherActiveTerms = await _termsRepository.GetListAsync(
            new QuerySpecification<TermsAndConditions>(x =>
                x.Type == type &&
                x.IsActive &&
                x.Id != activeTermsId));

        foreach (var item in otherActiveTerms)
        {
            item.Deactivate();
        }
    }

    private async Task<TermsAndConditions?> GetLatestActiveTermByTypeAsync(string type)
    {
        return (await _termsRepository.GetListAsync(
            new QuerySpecification<TermsAndConditions>(x =>
                    x.Type == type &&
                    x.IsActive &&
                    x.EffectiveDate <= DateTime.UtcNow)
                .OrderByDescending(x => x.EffectiveDate)))
            .FirstOrDefault();
    }

    private static string NormalizeRequiredType(string type)
    {
        var normalized = NormalizeTypeOrNull(type);
        if (normalized == null)
        {
            throw new DomainException("Terms type is required.");
        }

        return normalized;
    }

    private static string? NormalizeTypeOrNull(string? type)
    {
        return string.IsNullOrWhiteSpace(type) ? null : type.Trim();
    }

    private async Task NotifyUsersAboutUpdatedTermsAsync(TermsAndConditions terms)
    {
        var loginUrl = $"{(_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/')}/auth/login";
        var documentName = GetDocumentDisplayName(terms.Type);
        var subject = _localizer["ComplianceReacceptanceEmailSubject", documentName].Value;
        var effectiveDate = terms.EffectiveDate.ToString("yyyy-MM-dd");
        var summary = terms.Summary.CurrentValue;
        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = documentName;
        }

        var recipients = await _userManager.Users
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                user.EmailConfirmed &&
                !string.IsNullOrWhiteSpace(user.Email))
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FirstName
            })
            .ToListAsync();

        foreach (var recipient in recipients)
        {
            var body = _localizer[
                "ComplianceReacceptanceEmailBody",
                string.IsNullOrWhiteSpace(recipient.FirstName) ? recipient.Email! : recipient.FirstName,
                documentName,
                terms.Version,
                effectiveDate,
                summary,
                loginUrl].Value;

            var result = await _notificationService.SendAsync(new NotificationMessage
            {
                Recipient = recipient.Email!,
                Title = subject,
                Body = body,
                Channel = NotificationChannel.Email,
                Data = new Dictionary<string, object>
                {
                    ["CorrelationId"] = Guid.NewGuid().ToString("N"),
                    ["RequestedByUserId"] = recipient.Id.ToString(),
                    ["ComplianceTermsId"] = terms.Id.ToString(),
                    ["ComplianceTermsType"] = terms.Type,
                    ["ComplianceTermsVersion"] = terms.Version,
                }
            });

            if (!result.Success)
            {
                // Best-effort notification: activation should still succeed even if a mailbox fails.
                continue;
            }
        }
    }

    private string GetDocumentDisplayName(string type)
    {
        return type switch
        {
            "TermsOfService" => _localizer["ComplianceDocumentTermsOfService"].Value,
            "PrivacyPolicy" => _localizer["ComplianceDocumentPrivacyPolicy"].Value,
            _ => type
        };
    }
}
