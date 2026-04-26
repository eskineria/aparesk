using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Core.Compliance.Models;

namespace Aparesk.Eskineria.Core.Compliance.Abstractions;

public interface IComplianceService
{
    // Terms Management
    Task<DataResponse<List<TermsDto>>> GetAllTermsAsync(string? type = null);
    Task<DataResponse<TermsDto>> GetActiveTermsByTypeAsync(string type);
    Task<DataResponse<TermsDto>> GetTermsByIdAsync(Guid id);
    Task<DataResponse<TermsDto>> CreateTermsAsync(CreateTermsDto dto);
    Task<Response> UpdateTermsAsync(Guid id, UpdateTermsDto dto);
    Task<Response> DeleteTermsAsync(Guid id);
    Task<Response> ActivateTermsAsync(Guid id);
    
    // User Acceptance
    Task<Response> AcceptTermsAsync(Guid userId, Guid termsAndConditionsId, string? ipAddress, string? userAgent);
    Task<DataResponse<List<UserTermsAcceptanceDto>>> GetUserAcceptancesAsync(Guid userId);
    Task<DataResponse<bool>> HasUserAcceptedLatestTermsAsync(Guid userId, string type);
    Task<DataResponse<List<TermsDto>>> GetPendingRequiredTermsAsync(Guid userId);
}
