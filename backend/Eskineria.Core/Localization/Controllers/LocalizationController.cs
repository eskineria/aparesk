using Microsoft.AspNetCore.Mvc;
using Eskineria.Core.Localization.Entities;
using Eskineria.Core.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;
using Eskineria.Core.Localization.Abstractions;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Eskineria.Core.Auth.Abstractions;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Localization.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class LocalizationController : ControllerBase
{
    private readonly ILocalizationService _localizationService;
    private readonly ILocalizationCacheInvalidator _localizationCacheInvalidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<LocalizationController> _localizer;

    public LocalizationController(
        ILocalizationService localizationService,
        ILocalizationCacheInvalidator localizationCacheInvalidator,
        ICurrentUserService currentUserService,
        IStringLocalizer<LocalizationController> localizer)
    {
        _localizationService = localizationService;
        _localizationCacheInvalidator = localizationCacheInvalidator;
        _currentUserService = currentUserService;
        _localizer = localizer;
    }

    [HttpGet("resources")]
    [AllowAnonymous]
    [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "lang" })]
    public async Task<IActionResult> GetResources([FromQuery] string lang, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return BadRequest(_localizer[LocalizationKeys.LanguageCodeRequired]);
        }

        var resources = await _localizationService.GetResourcesAsync(lang, cancellationToken);
        return Ok(resources);
    }

    [HttpGet]
    [HasPermission("Localization", "Read")]
    public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] string? culture, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _localizationService.GetListAsync(search, culture, page, pageSize);
        return Ok(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet("{id}")]
    [HasPermission("Localization", "Read")]
    public async Task<IActionResult> Get(int id)
    {
        var resource = await _localizationService.GetByIdAsync(id);
        if (resource == null) return NotFound();
        return Ok(resource);
    }

    [HttpGet("capabilities")]
    [HasPermission("Localization", "Read")]
    public IActionResult GetCapabilities()
    {
        var result = new LocalizationCapabilitiesResponse(
            DraftPublishEnabled: true,
            WorkflowEnabled: false,
            MissingTranslationBannerEnabled: true);

        return Ok(result);
    }

    [HttpPost]
    [HasPermission("Localization", "Manage")]
    public async Task<IActionResult> Create([FromBody] CreateLocalizationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Culture))
        {
            return BadRequest(new { message = _localizer[LocalizationKeys.CultureRequired].Value });
        }

        const bool saveAsDraft = true;
        LocalizationCreateResult result;
        try
        {
            result = await _localizationService.CreateAsync(request, saveAsDraft, _currentUserService.UserId, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (result.IsDuplicate)
            return BadRequest(_localizer[LocalizationKeys.LocalizationKeyAlreadyExistsForCulture]);

        if (result.Resource == null)
            return BadRequest(_localizer[LocalizationKeys.LocalizationCreateFailed]);

        return CreatedAtAction(nameof(Get), new { id = result.Resource.Id }, result.Resource);
    }

    [HttpPut("{id}")]
    [HasPermission("Localization", "Manage")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLocalizationRequest request, CancellationToken cancellationToken)
    {
        const bool saveAsDraft = true;
        var existing = await _localizationService.UpdateValueAsync(
            id,
            request.Value,
            saveAsDraft,
            _currentUserService.UserId,
            cancellationToken);
        if (existing == null) return NotFound();

        return Ok(existing);
    }

    [HttpPost("{id:int}/publish")]
    [HasPermission("Localization", "Manage")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var publishResult = await _localizationService.PublishAsync(
            new[] { id },
            _currentUserService.UserId.Value,
            cancellationToken);

        if (publishResult.PublishedCount == 0)
        {
            return NotFound(_localizer[LocalizationKeys.LocalizationResourceNotFound]);
        }

        _localizationCacheInvalidator.Clear();
        return Ok(publishResult);
    }

    [HttpGet("missing-keys")]
    [HasPermission("Localization", "Read")]
    public async Task<IActionResult> GetMissingKeys([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        var result = await _localizationService.GetMissingKeysAsync(culture, cancellationToken);
        return Ok(new LocalizationMissingKeysResponse(
            FeatureEnabled: true,
            RequestedCulture: result.RequestedCulture,
            MatchedCulture: result.MatchedCulture,
            FallbackCulture: result.FallbackCulture,
            MissingKeys: result.MissingKeys));
    }

    [HttpDelete("{id}")]
    [HasPermission("Localization", "Manage")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _localizationService.DeleteAsync(id);
        if (!deleted) return NotFound();
        _localizationCacheInvalidator.Clear();
        return NoContent();
    }

    [HttpDelete("cultures/{culture}")]
    [HasPermission("Localization", "Manage")]
    public async Task<IActionResult> DeleteCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return BadRequest(new { message = _localizer[LocalizationKeys.CultureRequired].Value });

        var result = await _localizationService.DeleteCultureAsync(culture);
        if (!result.Success)
        {
            if (result.FailureReason == LocalizationDeleteCultureFailureReason.LastCultureMustRemain)
            {
                return BadRequest(new { message = _localizer[LocalizationKeys.LocalizationLastCultureMustRemain].Value });
            }

            if (result.FailureReason == LocalizationDeleteCultureFailureReason.CultureNotFound)
            {
                return NotFound(new { message = _localizer[LocalizationKeys.LocalizationCultureNotFound, culture].Value });
            }

            if (result.FailureReason == LocalizationDeleteCultureFailureReason.NoResourcesFound)
            {
                return NotFound(new { message = _localizer[LocalizationKeys.LocalizationNoResourcesFoundForCulture, result.MatchedCulture ?? culture].Value });
            }

            return NotFound(new { message = result.ErrorMessage });
        }

        _localizationCacheInvalidator.Clear();

        return Ok(new
        {
            culture = result.MatchedCulture,
            deletedCount = result.DeletedCount,
            message = _localizer[LocalizationKeys.CultureDeletedSuccessfully, result.MatchedCulture ?? culture].Value
        });
    }

    [HttpGet("cultures")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCultures()
    {
        var cultures = await _localizationService.GetCulturesAsync();
        return Ok(cultures);
    }

    [HttpPost("clone")]
    [HasPermission("Localization", "Manage")]
    public async Task<IActionResult> CloneCulture([FromBody] CloneCultureRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceCulture) || string.IsNullOrWhiteSpace(request.TargetCulture))
            return BadRequest(_localizer[LocalizationKeys.SourceAndTargetCulturesRequired]);

        var sourceCulture = request.SourceCulture.Trim();
        var targetCulture = request.TargetCulture.Trim();

        if (string.Equals(sourceCulture, targetCulture, StringComparison.OrdinalIgnoreCase))
            return BadRequest(_localizer[LocalizationKeys.SourceAndTargetCulturesCannotBeSame]);

        LocalizationCloneCultureResult cloneResult;
        try
        {
            cloneResult = await _localizationService.CloneCultureAsync(sourceCulture, targetCulture, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (cloneResult.SourceCultureMissing)
            return BadRequest(_localizer[LocalizationKeys.NoResourcesFoundForSourceCulture, sourceCulture]);

        if (cloneResult.ClonedCount > 0)
        {
            _localizationCacheInvalidator.Clear();
        }

        return Ok(new
        {
            message = _localizer[LocalizationKeys.LocalizationCloneCompleted, cloneResult.ClonedCount, targetCulture].Value,
            count = cloneResult.ClonedCount
        });
    }
}

public class CloneCultureRequest
{
    public string SourceCulture { get; set; } = null!;
    public string TargetCulture { get; set; } = null!;
}

public sealed record LocalizationCapabilitiesResponse(
    bool DraftPublishEnabled,
    bool WorkflowEnabled,
    bool MissingTranslationBannerEnabled);

public sealed record LocalizationMissingKeysResponse(
    bool FeatureEnabled,
    string RequestedCulture,
    string? MatchedCulture,
    string? FallbackCulture,
    List<string> MissingKeys);
