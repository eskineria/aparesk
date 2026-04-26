using System.Security.Claims;
using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Auth.Models;

namespace Aparesk.Eskineria.Core.Auth.Abstractions;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokensAsync(
        EskineriaUser user,
        int? accessTokenLifetimeMinutes = null,
        int? refreshTokenLifetimeDays = null,
        int? maxActiveSessions = null);
    Task<TokenResponse> RefreshTokenAsync(
        string token,
        string refreshToken,
        int? accessTokenLifetimeMinutes = null,
        int? refreshTokenLifetimeDays = null,
        int? maxActiveSessions = null);
    Task<ClaimsPrincipal?> GetPrincipalFromTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeUserRefreshTokensAsync(Guid userId);
    Task<Guid?> GetSessionIdAsync(string refreshToken);
    Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(Guid userId, Guid? currentSessionId);
    Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, string? reason = null);
    Task<int> RevokeOtherSessionsAsync(Guid userId, Guid currentSessionId, string? reason = null);
}
