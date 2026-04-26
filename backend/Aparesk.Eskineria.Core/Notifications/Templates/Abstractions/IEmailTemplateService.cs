using Aparesk.Eskineria.Core.Notifications.Templates.Models;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Core.Notifications.Templates.Abstractions;

public interface IEmailTemplateService
{
    Task<DataResponse<List<EmailTemplateDto>>> GetAllAsync(string? culture = null);
    Task<DataResponse<EmailTemplateDto>> GetByIdAsync(int id);
    Task<DataResponse<EmailTemplateDto>> CreateAsync(CreateEmailTemplateRequest request);
    Task<DataResponse<EmailTemplateDto>> UpdateAsync(int id, UpdateEmailTemplateRequest request);
    Task<DataResponse<EmailTemplateDto>> PublishAsync(int id, PublishEmailTemplateRequest request);
    Task<DataResponse<EmailTemplateDto>> RollbackAsync(int id, RollbackEmailTemplateRequest request);
    Task<DataResponse<List<EmailTemplateRevisionDto>>> GetVersionsAsync(int id);
    Task<DataResponse<List<EmailTemplateCoverageItemDto>>> GetCoverageAsync();
    Task<DataResponse<EmailTemplateValidationResultDto>> ValidateAsync(ValidateEmailTemplateRequest request);
    Task<Response> SendTestAsync(SendEmailTemplateTestRequest request);
    Task<DataResponse<AutoTranslateEmailTemplateResultDto>> AutoTranslateAsync(AutoTranslateEmailTemplateRequest request);
    Task<DataResponse<AutoTranslateEmailTemplateResultDto>> AutoTranslateCultureAsync(
        string sourceCulture,
        string targetCulture,
        bool overwriteExisting);
    Task<Response> DeleteAsync(int id);
}
