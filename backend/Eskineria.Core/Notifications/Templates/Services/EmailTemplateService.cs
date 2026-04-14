using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Eskineria.Core.Notifications.Templates.Abstractions;
using Eskineria.Core.Notifications.Templates.Models;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Localization.Abstractions;
using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;
using Eskineria.Core.Notifications.Abstractions;
using Eskineria.Core.Notifications.Email;
using Eskineria.Core.Notifications.Models;
using Eskineria.Core.Repository.Specification;
using Eskineria.Core.Shared.Response;
using Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Notifications.Templates.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private const string DefaultCulture = "en-US";
    private const string DraftSaveChangeSource = "DraftSave";
    private const string PublishChangeSource = "Publish";
    private const string RollbackChangeSource = "Rollback";
    private const string AutoTranslateChangeSource = "AutoTranslate";

    private static readonly Regex TemplateVariableRegex =
        new(@"{{\s*([a-zA-Z0-9_]+)\s*}}", RegexOptions.Compiled);

    private static readonly Regex VariableNameRegex =
        new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    private static readonly Regex[] DangerousPatterns =
    {
        new Regex(@"<\s*script\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"vbscript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<\s*iframe\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<\s*object\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<\s*embed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"data\s*:\s*text/html", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IEmailTemplateRevisionRepository _emailTemplateRevisionRepository;
    private readonly ILanguageResourceRepository _languageResourceRepository;
    private readonly IStringLocalizer<EmailTemplateService> _localizer;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly INotificationService _notificationService;
    private readonly IAuditingStore _auditingStore;
    private readonly ICurrentUserService _currentUserService;

    public EmailTemplateService(
        IEmailTemplateRepository emailTemplateRepository,
        IEmailTemplateRevisionRepository emailTemplateRevisionRepository,
        ILanguageResourceRepository languageResourceRepository,
        IStringLocalizer<EmailTemplateService> localizer,
        ITemplateRenderer templateRenderer,
        INotificationService notificationService,
        IAuditingStore auditingStore,
        ICurrentUserService currentUserService)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _emailTemplateRevisionRepository = emailTemplateRevisionRepository;
        _languageResourceRepository = languageResourceRepository;
        _localizer = localizer;
        _templateRenderer = templateRenderer;
        _notificationService = notificationService;
        _auditingStore = auditingStore;
        _currentUserService = currentUserService;
    }

    public async Task<DataResponse<List<EmailTemplateDto>>> GetAllAsync(string? culture = null)
    {
        var normalizedCulture = string.IsNullOrWhiteSpace(culture) ? null : NormalizeCulture(culture);
        var entities = (await _emailTemplateRepository.GetListAsync(
                new QuerySpecification<EmailTemplate>(x =>
                    normalizedCulture == null || x.Culture == normalizedCulture)
                    .OrderBy(x => x.Key)))
            .OrderBy(x => x.Key)
            .ThenBy(x => x.Culture)
            .ToList();

        var templates = entities
            .Select(ToDto)
            .ToList();

        return DataResponse<List<EmailTemplateDto>>.Succeed(
            templates,
            _localizer[LocalizationKeys.EmailTemplatesRetrievedSuccessfully]);
    }

    public async Task<DataResponse<EmailTemplateDto>> GetByIdAsync(int id)
    {
        var template = await _emailTemplateRepository.GetByIdAsync(id);

        if (template == null)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateNotFound],
                StatusCodes.Status404NotFound);
        }

        return DataResponse<EmailTemplateDto>.Succeed(
            ToDto(template),
            _localizer[LocalizationKeys.EmailTemplateRetrievedSuccessfully]);
    }

    public async Task<DataResponse<EmailTemplateDto>> CreateAsync(CreateEmailTemplateRequest request)
    {

        var validation = await ValidateTemplateAsync(
            request.Subject,
            request.Body,
            request.RequiredVariables,
            enforceExtraVariablesAsError: request.RequiredVariables is { Count: > 0 });

        var validationError = ValidateCreateRequest(request, validation);
        if (validationError != null)
        {
            return DataResponse<EmailTemplateDto>.Fail(validationError, StatusCodes.Status400BadRequest);
        }

        var normalizedKey = request.Key.Trim();
        var normalizedCulture = NormalizeCulture(request.Culture);

        var keyExists = await _emailTemplateRepository.GetAsync(
            x => x.Key == normalizedKey && x.Culture == normalizedCulture) != null;

        if (keyExists)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateKeyAlreadyExists],
                StatusCodes.Status409Conflict);
        }

        var now = DateTime.UtcNow;
        var template = new EmailTemplate
        {
            Key = normalizedKey,
            Culture = normalizedCulture,
            Name = request.Name.Trim(),
            Subject = request.Subject.Trim(),
            Body = request.Body,
            RequiredVariables = SerializeRequiredVariables(validation.NormalizedRequiredVariables),
            IsActive = request.IsActive,
            IsDraft = true,
            CurrentVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.PublishNow)
        {
            template.IsDraft = false;
            template.PublishedVersion = template.CurrentVersion;
            template.PublishedAt = now;
            template.PublishedByUserId = GetCurrentUserIdAsString();
        }

        await _emailTemplateRepository.AddAsync(template);
        await _emailTemplateRepository.SaveChangesAsync();

        await _emailTemplateRevisionRepository.AddAsync(new EmailTemplateRevision
        {
            EmailTemplateId = template.Id,
            Version = template.CurrentVersion,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            RequiredVariables = template.RequiredVariables,
            IsPublishedSnapshot = request.PublishNow,
            ChangeSource = request.PublishNow ? PublishChangeSource : DraftSaveChangeSource,
            ChangedByUserId = GetCurrentUserIdAsString(),
            CreatedAt = now
        });

        await _emailTemplateRevisionRepository.SaveChangesAsync();

        await SaveAuditAsync(
            methodName: nameof(CreateAsync),
            parameters: $"TemplateId: {template.Id}, Key: {template.Key}, Culture: {template.Culture}, PublishNow: {request.PublishNow}");

        return DataResponse<EmailTemplateDto>.Succeed(
            ToDto(template),
            _localizer[LocalizationKeys.EmailTemplateCreatedSuccessfully],
            StatusCodes.Status201Created);
    }

    public async Task<DataResponse<EmailTemplateDto>> UpdateAsync(int id, UpdateEmailTemplateRequest request)
    {

        var template = await _emailTemplateRepository.GetByIdAsync(id);

        if (template == null)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateNotFound],
                StatusCodes.Status404NotFound);
        }

        var validation = await ValidateTemplateAsync(
            request.Subject,
            request.Body,
            request.RequiredVariables,
            enforceExtraVariablesAsError: request.RequiredVariables is { Count: > 0 });

        var validationError = ValidateUpdateRequest(request, validation);
        if (validationError != null)
        {
            return DataResponse<EmailTemplateDto>.Fail(validationError, StatusCodes.Status400BadRequest);
        }

        template.Name = request.Name.Trim();
        template.Subject = request.Subject.Trim();
        template.Body = request.Body;
        template.RequiredVariables = SerializeRequiredVariables(validation.NormalizedRequiredVariables);
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        template.CurrentVersion += 1;
        template.IsDraft = true;

        var now = DateTime.UtcNow;
        var changedByUserId = GetCurrentUserIdAsString();

        var revision = new EmailTemplateRevision
        {
            EmailTemplateId = template.Id,
            Version = template.CurrentVersion,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            RequiredVariables = template.RequiredVariables,
            IsPublishedSnapshot = false,
            ChangeSource = DraftSaveChangeSource,
            ChangedByUserId = changedByUserId,
            CreatedAt = now
        };

        if (request.PublishNow)
        {
            template.IsDraft = false;
            template.PublishedVersion = template.CurrentVersion;
            template.PublishedAt = now;
            template.PublishedByUserId = changedByUserId;
            revision.IsPublishedSnapshot = true;
            revision.ChangeSource = PublishChangeSource;
        }

        await _emailTemplateRevisionRepository.AddAsync(revision);
        await _emailTemplateRepository.SaveChangesAsync();

        await SaveAuditAsync(
            methodName: nameof(UpdateAsync),
            parameters: $"TemplateId: {template.Id}, Version: {template.CurrentVersion}, PublishNow: {request.PublishNow}");

        return DataResponse<EmailTemplateDto>.Succeed(
            ToDto(template),
            _localizer[LocalizationKeys.EmailTemplateUpdatedSuccessfully]);
    }

    public async Task<DataResponse<EmailTemplateDto>> PublishAsync(int id, PublishEmailTemplateRequest request)
    {
        var template = await _emailTemplateRepository.GetByIdAsync(id);

        if (template == null)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateNotFound],
                StatusCodes.Status404NotFound);
        }

        var validation = await ValidateTemplateAsync(
            template.Subject,
            template.Body,
            DeserializeRequiredVariables(template.RequiredVariables),
            enforceExtraVariablesAsError: false);

        if (!validation.IsValid)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                string.Join(" ", validation.Errors),
                StatusCodes.Status400BadRequest);
        }

        if (request.MarkActive)
        {
            template.IsActive = true;
        }

        var revision = await SpecificationEvaluator<EmailTemplateRevision>.GetQuery(
                _emailTemplateRevisionRepository.Query(asNoTracking: false),
                new QuerySpecification<EmailTemplateRevision>(
                    x => x.EmailTemplateId == template.Id && x.Version == template.CurrentVersion))
            .FirstOrDefaultAsync();

        if (revision == null)
        {
            revision = new EmailTemplateRevision
            {
                EmailTemplateId = template.Id,
                Version = template.CurrentVersion,
                Name = template.Name,
                Subject = template.Subject,
                Body = template.Body,
                RequiredVariables = template.RequiredVariables,
                IsPublishedSnapshot = true,
                ChangeSource = PublishChangeSource,
                ChangedByUserId = GetCurrentUserIdAsString(),
                CreatedAt = DateTime.UtcNow
            };
            await _emailTemplateRevisionRepository.AddAsync(revision);
        }
        else
        {
            revision.IsPublishedSnapshot = true;
            revision.ChangeSource = PublishChangeSource;
            revision.ChangedByUserId = GetCurrentUserIdAsString();
            await _emailTemplateRevisionRepository.UpdateAsync(revision);
        }

        template.IsDraft = false;
        template.PublishedVersion = template.CurrentVersion;
        template.PublishedAt = DateTime.UtcNow;
        template.PublishedByUserId = GetCurrentUserIdAsString();
        template.UpdatedAt = DateTime.UtcNow;

        await _emailTemplateRepository.SaveChangesAsync();

        await SaveAuditAsync(
            methodName: nameof(PublishAsync),
            parameters: $"TemplateId: {template.Id}, Version: {template.CurrentVersion}, MarkActive: {request.MarkActive}");

        return DataResponse<EmailTemplateDto>.Succeed(
            ToDto(template),
            _localizer[LocalizationKeys.EmailTemplateUpdatedSuccessfully]);
    }

    public async Task<DataResponse<EmailTemplateDto>> RollbackAsync(int id, RollbackEmailTemplateRequest request)
    {
        if (request.Version <= 0)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateVersionMustBeGreaterThanZero],
                StatusCodes.Status400BadRequest);
        }

        var template = await _emailTemplateRepository.GetByIdAsync(id);

        if (template == null)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateNotFound],
                StatusCodes.Status404NotFound);
        }

        var sourceRevision = await _emailTemplateRevisionRepository.GetAsync(
            x => x.EmailTemplateId == template.Id && x.Version == request.Version);

        if (sourceRevision == null)
        {
            return DataResponse<EmailTemplateDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateRequestedVersionNotFound],
                StatusCodes.Status404NotFound);
        }

        template.Name = sourceRevision.Name;
        template.Subject = sourceRevision.Subject;
        template.Body = sourceRevision.Body;
        template.RequiredVariables = sourceRevision.RequiredVariables;
        template.CurrentVersion += 1;
        template.UpdatedAt = DateTime.UtcNow;
        template.IsDraft = true;

        var now = DateTime.UtcNow;
        var changedBy = GetCurrentUserIdAsString();
        var newRevision = new EmailTemplateRevision
        {
            EmailTemplateId = template.Id,
            Version = template.CurrentVersion,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            RequiredVariables = template.RequiredVariables,
            IsPublishedSnapshot = false,
            ChangeSource = RollbackChangeSource,
            ChangedByUserId = changedBy,
            CreatedAt = now
        };

        if (request.PublishNow)
        {
            template.IsDraft = false;
            template.PublishedVersion = template.CurrentVersion;
            template.PublishedAt = now;
            template.PublishedByUserId = changedBy;
            newRevision.IsPublishedSnapshot = true;
            newRevision.ChangeSource = PublishChangeSource;
        }

        await _emailTemplateRevisionRepository.AddAsync(newRevision);
        await _emailTemplateRepository.SaveChangesAsync();

        await SaveAuditAsync(
            methodName: nameof(RollbackAsync),
            parameters: $"TemplateId: {template.Id}, FromVersion: {request.Version}, ToVersion: {template.CurrentVersion}, PublishNow: {request.PublishNow}");

        return DataResponse<EmailTemplateDto>.Succeed(
            ToDto(template),
            _localizer[LocalizationKeys.EmailTemplateRollbackSuccessful]);
    }

    public async Task<DataResponse<List<EmailTemplateRevisionDto>>> GetVersionsAsync(int id)
    {
        var templateExists = await _emailTemplateRepository.GetByIdAsync(id) != null;

        if (!templateExists)
        {
            return DataResponse<List<EmailTemplateRevisionDto>>.Fail(
                _localizer[LocalizationKeys.EmailTemplateNotFound],
                StatusCodes.Status404NotFound);
        }

        var entities = (await _emailTemplateRevisionRepository.GetListAsync(
                new QuerySpecification<EmailTemplateRevision>(x => x.EmailTemplateId == id)
                    .OrderByDescending(x => x.Version)))
            .OrderByDescending(x => x.Version)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var versions = entities
            .Select(x => new EmailTemplateRevisionDto
            {
                Id = x.Id,
                Version = x.Version,
                Name = x.Name,
                Subject = x.Subject,
                Body = x.Body,
                RequiredVariables = DeserializeRequiredVariables(x.RequiredVariables),
                IsPublishedSnapshot = x.IsPublishedSnapshot,
                ChangeSource = x.ChangeSource,
                ChangedByUserId = x.ChangedByUserId,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return DataResponse<List<EmailTemplateRevisionDto>>.Succeed(
            versions,
            _localizer[LocalizationKeys.EmailTemplateVersionsRetrievedSuccessfully]);
    }

    public async Task<DataResponse<List<EmailTemplateCoverageItemDto>>> GetCoverageAsync()
    {
        var templateItems = await _emailTemplateRepository.Query()
            .Select(x => new { x.Key, x.Culture, x.IsDraft })
            .ToListAsync();

        var cultures = await _languageResourceRepository.Query()
            .Select(x => x.Culture)
            .Distinct()
            .ToListAsync();

        if (cultures.Count == 0)
        {
            cultures = templateItems.Select(x => x.Culture).Distinct().ToList();
        }

        if (cultures.Count == 0)
        {
            cultures.Add(DefaultCulture);
        }

        var normalizedCultures = cultures
            .Select(NormalizeCulture)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var coverage = templateItems
            .GroupBy(x => x.Key)
            .Select(group =>
            {
                var templateCultures = group
                    .Select(x => NormalizeCulture(x.Culture))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingCultures = normalizedCultures
                    .Where(culture => !templateCultures.Contains(culture))
                    .OrderBy(x => x)
                    .ToList();

                var draftCultures = group
                    .Where(x => x.IsDraft)
                    .Select(x => NormalizeCulture(x.Culture))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                return new EmailTemplateCoverageItemDto
                {
                    Key = group.Key,
                    TotalCultures = normalizedCultures.Count,
                    MissingCultures = missingCultures.Count,
                    MissingCultureCodes = missingCultures,
                    DraftCultureCodes = draftCultures
                };
            })
            .OrderBy(x => x.Key)
            .ToList();

        return DataResponse<List<EmailTemplateCoverageItemDto>>.Succeed(
            coverage,
            _localizer[LocalizationKeys.EmailTemplateCoverageRetrievedSuccessfully]);
    }

    public async Task<DataResponse<EmailTemplateValidationResultDto>> ValidateAsync(ValidateEmailTemplateRequest request)
    {
        var validation = await ValidateTemplateAsync(
            request.Subject,
            request.Body,
            request.RequiredVariables,
            enforceExtraVariablesAsError: request.RequiredVariables is { Count: > 0 });

        return DataResponse<EmailTemplateValidationResultDto>.Succeed(
            validation,
            validation.IsValid
                ? _localizer[LocalizationKeys.EmailTemplateValid]
                : _localizer[LocalizationKeys.EmailTemplateValidationErrors]);
    }

    public async Task<Response> SendTestAsync(SendEmailTemplateTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return Response.Fail(
                _localizer[LocalizationKeys.EmailTemplateRecipientEmailRequired],
                StatusCodes.Status400BadRequest);
        }

        if (!IsValidEmail(request.ToEmail))
        {
            return Response.Fail(
                _localizer[LocalizationKeys.EmailTemplateRecipientEmailInvalid],
                StatusCodes.Status400BadRequest);
        }

        var template = await ResolveTemplateForSendAsync(request.TemplateId, request.Key, request.Culture);
        if (template == null)
        {
            return Response.Fail(_localizer[LocalizationKeys.EmailTemplateNotFound], StatusCodes.Status404NotFound);
        }

        var sourceSubject = template.Subject;
        var sourceBody = template.Body;
        var sourceRequiredVariables = template.RequiredVariables;

        if (request.UsePublishedVersion && template.PublishedVersion.HasValue)
        {
            var publishedRevision = await _emailTemplateRevisionRepository.GetAsync(
                x => x.EmailTemplateId == template.Id && x.Version == template.PublishedVersion.Value);

            if (publishedRevision != null)
            {
                sourceSubject = publishedRevision.Subject;
                sourceBody = publishedRevision.Body;
                sourceRequiredVariables = publishedRevision.RequiredVariables;
            }
        }

        var requiredVariables = DeserializeRequiredVariables(sourceRequiredVariables);
        var sampleModel = BuildDefaultTemplateModel(template.Key, template.Culture);

        if (request.Variables is { Count: > 0 })
        {
            foreach (var item in request.Variables)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                sampleModel[item.Key.Trim()] = item.Value;
            }
        }

        foreach (var requiredVariable in requiredVariables)
        {
            if (!sampleModel.ContainsKey(requiredVariable))
            {
                sampleModel[requiredVariable] = $"sample_{requiredVariable}";
            }
        }

        string renderedSubject;
        string renderedBody;

        try
        {
            var renderModel = BuildRenderModel(sampleModel);
            renderedSubject = await _templateRenderer.RenderAsync(sourceSubject, renderModel);
            renderedBody = await _templateRenderer.RenderAsync(sourceBody, renderModel);
        }
        catch (Exception ex)
        {
            return Response.Fail(
                _localizer[LocalizationKeys.EmailTemplateRenderFailed, ex.Message],
                StatusCodes.Status400BadRequest);
        }

        var sendResult = await _notificationService.SendAsync(new NotificationMessage
        {
            Recipient = request.ToEmail.Trim(),
            Title = string.IsNullOrWhiteSpace(renderedSubject) ? sourceSubject : renderedSubject.Trim(),
            Body = string.IsNullOrWhiteSpace(renderedBody) ? sourceBody : renderedBody,
            Channel = NotificationChannel.Email,
            Data = new Dictionary<string, object>
            {
                ["TemplateKey"] = template.Key,
                ["Culture"] = template.Culture,
                ["CorrelationId"] = Guid.NewGuid().ToString("N"),
                ["RequestedByUserId"] = GetCurrentUserIdAsString() ?? string.Empty,
            }
        });

        if (!sendResult.Success)
        {
            return Response.Fail(
                string.IsNullOrWhiteSpace(sendResult.ErrorMessage)
                    ? _localizer[LocalizationKeys.EmailTemplateTestSendFailed]
                    : sendResult.ErrorMessage,
                StatusCodes.Status500InternalServerError);
        }

        await SaveAuditAsync(
            methodName: nameof(SendTestAsync),
            parameters: $"TemplateId: {template.Id}, ToEmail: {request.ToEmail}, UsePublishedVersion: {request.UsePublishedVersion}");

        return Response.Succeed(_localizer[LocalizationKeys.EmailTemplateTestSentSuccessfully]);
    }

    public async Task<DataResponse<AutoTranslateEmailTemplateResultDto>> AutoTranslateAsync(AutoTranslateEmailTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return DataResponse<AutoTranslateEmailTemplateResultDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateKeyRequired],
                StatusCodes.Status400BadRequest);
        }

        var sourceCulture = NormalizeCulture(request.SourceCulture);
        var targetCulture = NormalizeCulture(request.TargetCulture);

        if (string.Equals(sourceCulture, targetCulture, StringComparison.OrdinalIgnoreCase))
        {
            return DataResponse<AutoTranslateEmailTemplateResultDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateSourceAndTargetCultureSame],
                StatusCodes.Status400BadRequest);
        }

        var sourceTemplate = await _emailTemplateRepository.GetAsync(
            x => x.Key == request.Key.Trim() && x.Culture == sourceCulture);

        if (sourceTemplate == null)
        {
            return DataResponse<AutoTranslateEmailTemplateResultDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateSourceTemplateNotFoundForCulture],
                StatusCodes.Status404NotFound);
        }

        var result = await UpsertAutoTranslatedTemplateAsync(sourceTemplate, targetCulture, request.OverwriteExisting);

        await SaveAuditAsync(
            methodName: nameof(AutoTranslateAsync),
            parameters: $"Key: {request.Key}, SourceCulture: {sourceCulture}, TargetCulture: {targetCulture}, Overwrite: {request.OverwriteExisting}");

        return DataResponse<AutoTranslateEmailTemplateResultDto>.Succeed(
            result,
            _localizer[LocalizationKeys.EmailTemplateAutoTranslationDraftGeneratedSuccessfully]);
    }

    public async Task<DataResponse<AutoTranslateEmailTemplateResultDto>> AutoTranslateCultureAsync(
        string sourceCulture,
        string targetCulture,
        bool overwriteExisting)
    {
        var normalizedSourceCulture = NormalizeCulture(sourceCulture);
        var normalizedTargetCulture = NormalizeCulture(targetCulture);

        if (string.Equals(normalizedSourceCulture, normalizedTargetCulture, StringComparison.OrdinalIgnoreCase))
        {
            return DataResponse<AutoTranslateEmailTemplateResultDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateSourceAndTargetCultureSame],
                StatusCodes.Status400BadRequest);
        }

        var sourceTemplates = await _emailTemplateRepository.GetListAsync(
            new QuerySpecification<EmailTemplate>(x => x.Culture == normalizedSourceCulture));

        if (sourceTemplates.Count == 0)
        {
            return DataResponse<AutoTranslateEmailTemplateResultDto>.Fail(
                _localizer[LocalizationKeys.EmailTemplateNoSourceTemplatesFoundForCulture],
                StatusCodes.Status404NotFound);
        }

        var result = new AutoTranslateEmailTemplateResultDto();
        foreach (var sourceTemplate in sourceTemplates)
        {
            var itemResult = await UpsertAutoTranslatedTemplateAsync(sourceTemplate, normalizedTargetCulture, overwriteExisting);
            result.CreatedCount += itemResult.CreatedCount;
            result.UpdatedCount += itemResult.UpdatedCount;
            result.SkippedCount += itemResult.SkippedCount;
        }

        await SaveAuditAsync(
            methodName: nameof(AutoTranslateCultureAsync),
            parameters: $"SourceCulture: {normalizedSourceCulture}, TargetCulture: {normalizedTargetCulture}, Overwrite: {overwriteExisting}, Created: {result.CreatedCount}, Updated: {result.UpdatedCount}, Skipped: {result.SkippedCount}");

        return DataResponse<AutoTranslateEmailTemplateResultDto>.Succeed(
            result,
            _localizer[LocalizationKeys.EmailTemplateAutoTranslationDraftsGeneratedSuccessfully]);
    }

    public async Task<Response> DeleteAsync(int id)
    {
        var template = await _emailTemplateRepository.GetByIdAsync(id);

        if (template == null)
        {
            return Response.Fail(
                _localizer[LocalizationKeys.EmailTemplateNotFound],
                StatusCodes.Status404NotFound);
        }

        await _emailTemplateRepository.DeleteAsync(template);
        await _emailTemplateRepository.SaveChangesAsync();

        await SaveAuditAsync(
            methodName: nameof(DeleteAsync),
            parameters: $"TemplateId: {id}, Key: {template.Key}, Culture: {template.Culture}");

        return Response.Succeed(_localizer[LocalizationKeys.EmailTemplateDeletedSuccessfully]);
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return DefaultCulture;
        }

        var normalized = culture.Trim().Replace('_', '-');
        return string.IsNullOrWhiteSpace(normalized) ? DefaultCulture : normalized;
    }

    private static List<string> ExtractVariables(string? subject, string? body)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            foreach (Match match in TemplateVariableRegex.Matches(subject))
            {
                var variable = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(variable))
                {
                    output.Add(variable);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            foreach (Match match in TemplateVariableRegex.Matches(body))
            {
                var variable = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(variable))
                {
                    output.Add(variable);
                }
            }
        }

        return output.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    private static List<string> NormalizeRequiredVariables(IEnumerable<string>? rawVariables)
    {
        if (rawVariables == null)
        {
            return new List<string>();
        }

        return rawVariables
            .Select(x => x?.Trim() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<EmailTemplateValidationResultDto> ValidateTemplateAsync(
        string subject,
        string body,
        IEnumerable<string>? requiredVariables,
        bool enforceExtraVariablesAsError)
    {
        var result = new EmailTemplateValidationResultDto();

        if (string.IsNullOrWhiteSpace(subject))
        {
            result.Errors.Add(_localizer[LocalizationKeys.EmailTemplateSubjectRequired]);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            result.Errors.Add(_localizer[LocalizationKeys.EmailTemplateBodyRequired]);
        }

        result.UsedVariables = ExtractVariables(subject, body);
        result.NormalizedRequiredVariables = NormalizeRequiredVariables(requiredVariables);

        var invalidRequiredVariableNames = result.NormalizedRequiredVariables
            .Where(x => !VariableNameRegex.IsMatch(x))
            .ToList();

        if (invalidRequiredVariableNames.Count > 0)
        {
            result.Errors.Add(_localizer[
                LocalizationKeys.EmailTemplateInvalidRequiredVariableNames,
                string.Join(", ", invalidRequiredVariableNames)]);
        }

        if (result.NormalizedRequiredVariables.Count == 0)
        {
            result.NormalizedRequiredVariables = result.UsedVariables.ToList();
        }

        result.MissingRequiredVariables = result.NormalizedRequiredVariables
            .Except(result.UsedVariables, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        result.ExtraVariables = result.UsedVariables
            .Except(result.NormalizedRequiredVariables, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        if (result.MissingRequiredVariables.Count > 0)
        {
            result.Errors.Add(_localizer[
                LocalizationKeys.EmailTemplateMissingRequiredVariables,
                string.Join(", ", result.MissingRequiredVariables)]);
        }

        if (enforceExtraVariablesAsError && result.ExtraVariables.Count > 0)
        {
            result.Errors.Add(_localizer[
                LocalizationKeys.EmailTemplateUndeclaredTemplateVariables,
                string.Join(", ", result.ExtraVariables)]);
        }

        foreach (var pattern in DangerousPatterns)
        {
            if (!string.IsNullOrWhiteSpace(body) && pattern.IsMatch(body))
            {
                result.Errors.Add(_localizer[LocalizationKeys.EmailTemplateUnsafeContentDetected]);
                break;
            }
        }

        try
        {
            await _templateRenderer.RenderAsync(subject, BuildRenderModel(new Dictionary<string, string>()));
        }
        catch (Exception ex)
        {
            result.Errors.Add(_localizer[LocalizationKeys.EmailTemplateSubjectRenderError, ex.Message]);
        }

        try
        {
            await _templateRenderer.RenderAsync(body, BuildRenderModel(new Dictionary<string, string>()));
        }
        catch (Exception ex)
        {
            result.Errors.Add(_localizer[LocalizationKeys.EmailTemplateBodyRenderError, ex.Message]);
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private string? ValidateCreateRequest(CreateEmailTemplateRequest request, EmailTemplateValidationResultDto validation)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return _localizer[LocalizationKeys.EmailTemplateKeyRequired];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return _localizer[LocalizationKeys.EmailTemplateNameRequired];
        }

        if (string.IsNullOrWhiteSpace(request.Culture))
        {
            return _localizer[LocalizationKeys.CultureRequired];
        }

        if (!validation.IsValid)
        {
            return string.Join(" ", validation.Errors);
        }

        return null;
    }

    private string? ValidateUpdateRequest(UpdateEmailTemplateRequest request, EmailTemplateValidationResultDto validation)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return _localizer[LocalizationKeys.EmailTemplateNameRequired];
        }

        if (!validation.IsValid)
        {
            return string.Join(" ", validation.Errors);
        }

        return null;
    }

    private static string SerializeRequiredVariables(List<string> variables)
    {
        return JsonSerializer.Serialize(variables ?? new List<string>());
    }

    private static List<string> DeserializeRequiredVariables(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(value);
            return parsed?
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static EmailTemplateDto ToDto(EmailTemplate template)
    {
        return new EmailTemplateDto
        {
            Id = template.Id,
            Key = template.Key,
            Culture = template.Culture,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            RequiredVariables = DeserializeRequiredVariables(template.RequiredVariables),
            IsActive = template.IsActive,
            IsDraft = template.IsDraft,
            CurrentVersion = template.CurrentVersion,
            PublishedVersion = template.PublishedVersion,
            PublishedAt = template.PublishedAt,
            PublishedByUserId = template.PublishedByUserId,
            AutoTranslatedFromCulture = template.AutoTranslatedFromCulture,
            AutoTranslatedAt = template.AutoTranslatedAt,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private async Task SaveAuditAsync(string methodName, string parameters)
    {
        await _auditingStore.SaveAsync(new AuditLog
        {
            ServiceName = nameof(EmailTemplateService),
            MethodName = methodName,
            Parameters = JsonSerializer.Serialize(new
            {
                Before = (object?)null,
                After = ParseAuditParameters(parameters)
            }),
            UserId = GetCurrentUserIdAsString(),
            ExecutionTime = DateTime.UtcNow
        });
    }

    private static Dictionary<string, string?> ParseAuditParameters(string parameters)
    {
        var output = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return output;
        }

        var segments = parameters.Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf(':');
            if (separatorIndex < 0)
            {
                separatorIndex = segment.IndexOf('=');
            }

            if (separatorIndex <= 0 || separatorIndex >= segment.Length - 1)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            output[key] = value;
        }

        if (output.Count == 0)
        {
            output["Details"] = parameters.Trim();
        }

        return output;
    }

    private string? GetCurrentUserIdAsString()
    {
        return _currentUserService.UserId?.ToString();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var trimmed = email.Trim();
            var atIndex = trimmed.IndexOf('@');
            return atIndex > 0 && atIndex < trimmed.Length - 3 && trimmed.Contains('.');
        }
        catch
        {
            return false;
        }
    }

    private async Task<EmailTemplate?> ResolveTemplateForSendAsync(int? templateId, string? key, string? culture)
    {
        if (templateId.HasValue)
        {
            return await _emailTemplateRepository.GetByIdAsync(templateId.Value);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalizedCulture = NormalizeCulture(culture);
        var normalizedKey = key.Trim();

        var template = await _emailTemplateRepository.GetAsync(
            x => x.Key == normalizedKey && x.Culture == normalizedCulture);

        if (template != null)
        {
            return template;
        }

        return (await _emailTemplateRepository.GetListAsync(
                new QuerySpecification<EmailTemplate>(x => x.Key == normalizedKey)
                    .OrderBy(x => x.Culture)))
            .OrderBy(x => string.Equals(x.Culture, DefaultCulture, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(x => x.Culture)
            .FirstOrDefault();
    }

    private static ExpandoObject BuildRenderModel(Dictionary<string, string> variables)
    {
        IDictionary<string, object?> model = new ExpandoObject();
        foreach (var item in variables)
        {
            model[item.Key] = item.Value;
        }

        return (ExpandoObject)model;
    }

    private Dictionary<string, string> BuildDefaultTemplateModel(string templateKey, string culture)
    {
        var normalizedCulture = NormalizeCulture(culture);
        var sampleName = LocalizeForCulture(normalizedCulture, "EmailTemplateSampleName");

        var model = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["VerificationCodeEmailTitle"] = LocalizeForCulture(normalizedCulture, "VerificationCodeEmailTitle"),
            ["Greeting"] = LocalizeForCulture(normalizedCulture, "EmailGreeting", sampleName),
            ["VerificationCodeEmailContent"] = LocalizeForCulture(normalizedCulture, "VerificationCodeEmailContent"),
            ["VerificationCodeLabel"] = LocalizeForCulture(normalizedCulture, "VerificationCodeLabel"),
            ["VerificationCode"] = "742901",
            ["OpenVerificationPageButton"] = LocalizeForCulture(normalizedCulture, "OpenVerificationPageButton"),
            ["ConfirmPageUrl"] = "https://app.eskineria.com/confirm-email?email=john@example.com",
            ["EmailSecurityNote"] = LocalizeForCulture(normalizedCulture, "EmailSecurityNote"),
            ["VerificationCodeEmailExpiry"] = LocalizeForCulture(normalizedCulture, "VerificationCodeEmailExpiry", 180),
            ["EmailTeam"] = LocalizeForCulture(normalizedCulture, "EmailTeam"),
            ["EmailFooterIgnore"] = LocalizeForCulture(normalizedCulture, "EmailFooterIgnore"),
            ["reset_link"] = "https://app.eskineria.com/reset-password?token=sample-token",
            ["ResetPasswordEmailTitle"] = LocalizeForCulture(normalizedCulture, "ResetPasswordEmailTitle"),
            ["ResetPasswordEmailContent"] = LocalizeForCulture(normalizedCulture, "ResetPasswordEmailContent"),
            ["ResetPasswordButton"] = LocalizeForCulture(normalizedCulture, "ResetPasswordButton"),
            ["ResetPasswordEmailExpiry"] = LocalizeForCulture(normalizedCulture, "ResetPasswordEmailExpiry"),
            ["EmailSupportText"] = LocalizeForCulture(normalizedCulture, "EmailSupportText"),
            ["WelcomeEmailTitle"] = LocalizeForCulture(normalizedCulture, "WelcomeEmailTitle"),
            ["WelcomeEmailContent"] = LocalizeForCulture(normalizedCulture, "WelcomeEmailContent"),
            ["VerifyEmailButton"] = LocalizeForCulture(normalizedCulture, "VerifyEmailButton"),
            ["confirm_link"] = "https://app.eskineria.com/confirm-email?token=sample-token",
            ["PasswordChangedAlertEmailTitle"] = LocalizeForCulture(normalizedCulture, "PasswordChangedAlertEmailTitle"),
            ["PasswordChangedAlertEmailContent"] = LocalizeForCulture(normalizedCulture, "PasswordChangedAlertEmailContent"),
            ["SecurityEventTimeLabel"] = LocalizeForCulture(normalizedCulture, "SecurityEventTimeLabel"),
            ["SecurityEventLocationLabel"] = LocalizeForCulture(normalizedCulture, "SecurityEventLocationLabel"),
            ["SecurityEventDeviceLabel"] = LocalizeForCulture(normalizedCulture, "SecurityEventDeviceLabel"),
            ["EventDateTime"] = "2026-02-14 10:24:11 UTC",
            ["LoginLocation"] = "203.0.113.10",
            ["DeviceInfo"] = "Chrome / macOS",
            ["AccountSecurityActionText"] = LocalizeForCulture(normalizedCulture, "AccountSecurityActionText"),
            ["AccountSecurityActionLink"] = "https://app.eskineria.com/users/profile",
            ["AccountSecurityButton"] = LocalizeForCulture(normalizedCulture, "AccountSecurityButton"),
            ["ReviewSecurityButton"] = LocalizeForCulture(normalizedCulture, "ReviewSecurityButton"),
            ["LoginAlertEmailTitle"] = LocalizeForCulture(normalizedCulture, "LoginAlertEmailTitle"),
            ["LoginAlertEmailContent"] = LocalizeForCulture(normalizedCulture, "LoginAlertEmailContent"),
        };

        if (!string.IsNullOrWhiteSpace(templateKey) &&
            !model.ContainsKey(templateKey) &&
            templateKey.Contains("Title", StringComparison.OrdinalIgnoreCase))
        {
            model[templateKey] = templateKey;
        }

        return model;
    }

    private string LocalizeForCulture(string culture, string key, params object[] args)
    {
        var previousUICulture = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            var targetCulture = ResolveCultureOrDefault(culture);
            CultureInfo.CurrentUICulture = targetCulture;
            CultureInfo.CurrentCulture = targetCulture;

            var localized = args.Length == 0
                ? _localizer[key]
                : _localizer[key, args];

            return localized.Value;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUICulture;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static CultureInfo ResolveCultureOrDefault(string culture)
    {
        try
        {
            return CultureInfo.GetCultureInfo(NormalizeCulture(culture));
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(DefaultCulture);
        }
    }

    private async Task<AutoTranslateEmailTemplateResultDto> UpsertAutoTranslatedTemplateAsync(
        EmailTemplate sourceTemplate,
        string targetCulture,
        bool overwriteExisting)
    {
        var result = new AutoTranslateEmailTemplateResultDto();
        var now = DateTime.UtcNow;

        var targetTemplate = await SpecificationEvaluator<EmailTemplate>.GetQuery(
                _emailTemplateRepository.Query(asNoTracking: false),
                new QuerySpecification<EmailTemplate>(
                    x => x.Key == sourceTemplate.Key && x.Culture == targetCulture))
            .FirstOrDefaultAsync();

        if (targetTemplate == null)
        {
            targetTemplate = new EmailTemplate
            {
                Key = sourceTemplate.Key,
                Culture = targetCulture,
                Name = sourceTemplate.Name,
                Subject = sourceTemplate.Subject,
                Body = sourceTemplate.Body,
                RequiredVariables = sourceTemplate.RequiredVariables,
                IsActive = sourceTemplate.IsActive,
                IsDraft = true,
                CurrentVersion = 1,
                AutoTranslatedFromCulture = sourceTemplate.Culture,
                AutoTranslatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _emailTemplateRepository.AddAsync(targetTemplate);
            await _emailTemplateRepository.SaveChangesAsync();

            await _emailTemplateRevisionRepository.AddAsync(new EmailTemplateRevision
            {
                EmailTemplateId = targetTemplate.Id,
                Version = targetTemplate.CurrentVersion,
                Name = targetTemplate.Name,
                Subject = targetTemplate.Subject,
                Body = targetTemplate.Body,
                RequiredVariables = targetTemplate.RequiredVariables,
                IsPublishedSnapshot = false,
                ChangeSource = AutoTranslateChangeSource,
                ChangedByUserId = GetCurrentUserIdAsString(),
                CreatedAt = now
            });

            await _emailTemplateRevisionRepository.SaveChangesAsync();
            result.CreatedCount = 1;
            return result;
        }

        if (!overwriteExisting)
        {
            result.SkippedCount = 1;
            return result;
        }

        targetTemplate.Name = sourceTemplate.Name;
        targetTemplate.Subject = sourceTemplate.Subject;
        targetTemplate.Body = sourceTemplate.Body;
        targetTemplate.RequiredVariables = sourceTemplate.RequiredVariables;
        targetTemplate.IsDraft = true;
        targetTemplate.CurrentVersion += 1;
        targetTemplate.AutoTranslatedFromCulture = sourceTemplate.Culture;
        targetTemplate.AutoTranslatedAt = now;
        targetTemplate.UpdatedAt = now;

        await _emailTemplateRevisionRepository.AddAsync(new EmailTemplateRevision
        {
            EmailTemplateId = targetTemplate.Id,
            Version = targetTemplate.CurrentVersion,
            Name = targetTemplate.Name,
            Subject = targetTemplate.Subject,
            Body = targetTemplate.Body,
            RequiredVariables = targetTemplate.RequiredVariables,
            IsPublishedSnapshot = false,
            ChangeSource = AutoTranslateChangeSource,
            ChangedByUserId = GetCurrentUserIdAsString(),
            CreatedAt = now
        });

        await _emailTemplateRepository.SaveChangesAsync();
        result.UpdatedCount = 1;
        return result;
    }
}
