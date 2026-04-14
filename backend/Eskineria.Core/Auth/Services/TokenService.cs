using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Auth.Constants;
using Eskineria.Core.Auth.Configuration;
using Eskineria.Core.Auth.Data;
using Eskineria.Core.Auth.Entities;
using Eskineria.Core.Auth.Models;
using Eskineria.Core.Auth.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Eskineria.Core.Auth.Services;

public class TokenService : ITokenService
{
    private const string UserIdClaim = "id";
    private const int MinAccessTokenLifetimeMinutes = 5;
    private const int MaxAccessTokenLifetimeMinutes = 240;
    private const int MinRefreshTokenLifetimeDays = 1;
    private const int MaxRefreshTokenLifetimeDays = 90;
    private const int MinMaxActiveSessions = 1;
    private const int MaxMaxActiveSessions = 20;
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<EskineriaUser> _userManager;
    private readonly EskineriaIdentityDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonWebTokenHandler _tokenHandler;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        UserManager<EskineriaUser> userManager,
        EskineriaIdentityDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _tokenHandler = new JsonWebTokenHandler();

        if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
        {
            throw new InvalidOperationException("JwtSettings:Secret must be configured.");
        }
    }

    public async Task<TokenResponse> GenerateTokensAsync(
        EskineriaUser user,
        int? accessTokenLifetimeMinutes = null,
        int? refreshTokenLifetimeDays = null,
        int? maxActiveSessions = null)
    {
        var normalizedAccessTokenLifetimeMinutes = NormalizeAccessTokenLifetimeMinutes(accessTokenLifetimeMinutes ?? _jwtSettings.AccessTokenExpirationInMinutes);
        var normalizedRefreshTokenLifetimeDays = NormalizeRefreshTokenLifetimeDays(refreshTokenLifetimeDays ?? _jwtSettings.RefreshTokenExpirationInDays);
        var normalizedMaxActiveSessions = NormalizeOptionalMaxActiveSessions(maxActiveSessions);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email ?? "",
            [UserIdClaim] = user.Id.ToString()
        };

        if (!string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            claims[_userManager.Options.ClaimsIdentity.SecurityStampClaimType] = user.SecurityStamp;
        }

        if (!string.IsNullOrWhiteSpace(user.ActiveRole))
        {
            claims[CustomClaimTypes.ActiveRole] = user.ActiveRole;
        }
        
        // Add User Roles
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(user.ActiveRole) &&
                userRoles.Contains(user.ActiveRole, StringComparer.OrdinalIgnoreCase))
            {
                claims[ClaimTypes.Role] = new[] { user.ActiveRole };
            }
            else
            {
                claims[ClaimTypes.Role] = userRoles.ToArray();
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(normalizedAccessTokenLifetimeMinutes),
            SigningCredentials = credentials,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience
        };

        var accessToken = _tokenHandler.CreateToken(tokenDescriptor);

        var now = DateTime.UtcNow;
        var rawRefreshToken = GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            JwtId = _tokenHandler.ReadJsonWebToken(accessToken).Id,
            UserId = user.Id,
            CreationDate = now,
            LastUsedAtUtc = now,
            ExpiryDate = now.AddDays(normalizedRefreshTokenLifetimeDays),
            Token = HashRefreshToken(rawRefreshToken),
            IpAddress = TrimOrNull(GetCurrentIpAddress(), 64),
            UserAgent = TrimOrNull(GetCurrentUserAgent(), 1024)
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        if (normalizedMaxActiveSessions.HasValue)
        {
            await EnforceActiveSessionLimitAsync(user.Id, normalizedMaxActiveSessions.Value, refreshToken.Id);
        }

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiryDate = tokenDescriptor.Expires.Value,
            RefreshTokenExpiryDate = refreshToken.ExpiryDate
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<TokenResponse> RefreshTokenAsync(
        string token,
        string refreshToken,
        int? accessTokenLifetimeMinutes = null,
        int? refreshTokenLifetimeDays = null,
        int? maxActiveSessions = null)
    {
        var principal = await GetPrincipalFromTokenAsync(token);
        if (principal == null)
        {
            throw new SecurityTokenException("Invalid Token");
        }

        var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        
        var storedRefreshToken = await FindStoredRefreshTokenAsync(refreshToken);

        if (storedRefreshToken == null)
            throw new SecurityTokenException("Invalid refresh token");

        var now = DateTime.UtcNow;

        if (storedRefreshToken.ExpiryDate < now)
            throw new SecurityTokenException("Refresh token expired");

        if (storedRefreshToken.Invalidated || storedRefreshToken.Used)
        {
            await HandlePotentialRefreshTokenReplayAsync(storedRefreshToken);
            throw new SecurityTokenException("Refresh token status is invalid");
        }

        if (storedRefreshToken.JwtId != jti)
        {
            InvalidateSession(storedRefreshToken, "token_mismatch");
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();
            throw new SecurityTokenException("Token mismatch");
        }

        var userIdValue = principal.FindFirstValue(UserIdClaim) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new SecurityTokenException("Invalid token subject");

        if (storedRefreshToken.UserId != userId)
        {
            InvalidateSession(storedRefreshToken, "user_mismatch");
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();
            throw new SecurityTokenException("Refresh token user mismatch");
        }

        // Mark the current refresh token as rotated/revoked.
        storedRefreshToken.Used = true;
        storedRefreshToken.Invalidated = true;
        storedRefreshToken.LastUsedAtUtc = now;
        storedRefreshToken.InvalidatedAtUtc = now;
        storedRefreshToken.RevocationReason = "rotated";
        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new Exception("User not found");
        if (!user.IsActive) throw new SecurityTokenException("User is inactive");
        if (await _userManager.IsLockedOutAsync(user)) throw new SecurityTokenException("User is locked out");

        return await GenerateTokensAsync(
            user,
            accessTokenLifetimeMinutes,
            refreshTokenLifetimeDays,
            maxActiveSessions);
    }

    public async Task<ClaimsPrincipal?> GetPrincipalFromTokenAsync(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false // Here we don't care about lifetime for getting principal
            };

            var result = await _tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);
            if (!result.IsValid || result.SecurityToken is not JsonWebToken jsonWebToken || 
                !jsonWebToken.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return new ClaimsPrincipal(result.ClaimsIdentity);
        }
        catch
        {
            return null;
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var storedRefreshToken = await FindStoredRefreshTokenAsync(refreshToken);

        if (storedRefreshToken == null || storedRefreshToken.Invalidated)
        {
            return;
        }

        InvalidateSession(storedRefreshToken, "logout");
        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeUserRefreshTokensAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && !x.Invalidated)
            .ToListAsync();

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            InvalidateSession(token, "security_reset");
        }

        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync();
    }

    public async Task<Guid?> GetSessionIdAsync(string refreshToken)
    {
        var normalizedRefreshToken = NormalizeRefreshTokenInput(refreshToken);
        if (normalizedRefreshToken == null)
        {
            return null;
        }

        var hashedRefreshToken = HashRefreshToken(normalizedRefreshToken);
        var sessionId = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.Token == hashedRefreshToken)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        if (sessionId.HasValue)
        {
            return sessionId;
        }

        // Legacy fallback for previously persisted raw refresh tokens.
        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.Token == normalizedRefreshToken)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(Guid userId, Guid? currentSessionId)
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastUsedAtUtc ?? x.CreationDate)
            .Select(x => new UserSessionDto
            {
                Id = x.Id,
                CreatedAtUtc = x.CreationDate,
                LastUsedAtUtc = x.LastUsedAtUtc,
                ExpiresAtUtc = x.ExpiryDate,
                IsCurrent = false,
                IsRevoked = x.Invalidated || x.Used,
                IsExpired = x.ExpiryDate <= now,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .ToListAsync();

        if (currentSessionId.HasValue)
        {
            foreach (var session in sessions)
            {
                session.IsCurrent = session.Id == currentSessionId.Value;
            }
        }

        return sessions;
    }

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, string? reason = null)
    {
        var session = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);

        if (session == null)
        {
            return false;
        }

        if (session.Invalidated)
        {
            return true;
        }

        InvalidateSession(session, reason ?? "manual_revoke");
        _context.RefreshTokens.Update(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> RevokeOtherSessionsAsync(Guid userId, Guid currentSessionId, string? reason = null)
    {
        var sessions = await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.Id != currentSessionId && !x.Invalidated)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return 0;
        }

        foreach (var session in sessions)
        {
            InvalidateSession(session, reason ?? "revoke_others");
        }

        _context.RefreshTokens.UpdateRange(sessions);
        await _context.SaveChangesAsync();
        return sessions.Count;
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private async Task EnforceActiveSessionLimitAsync(Guid userId, int maxActiveSessions, Guid currentSessionId)
    {
        var now = DateTime.UtcNow;
        var activeSessions = await _context.RefreshTokens
            .Where(x => x.UserId == userId && !x.Invalidated && x.ExpiryDate > now)
            .OrderByDescending(x => x.LastUsedAtUtc ?? x.CreationDate)
            .ToListAsync();

        if (activeSessions.Count <= maxActiveSessions)
        {
            return;
        }

        var sessionsToKeep = new HashSet<Guid> { currentSessionId };
        foreach (var session in activeSessions)
        {
            if (sessionsToKeep.Count >= maxActiveSessions)
            {
                break;
            }

            if (session.Id == currentSessionId)
            {
                continue;
            }

            sessionsToKeep.Add(session.Id);
        }

        var sessionsToInvalidate = activeSessions
            .Where(x => !sessionsToKeep.Contains(x.Id))
            .ToList();

        if (sessionsToInvalidate.Count == 0)
        {
            return;
        }

        foreach (var session in sessionsToInvalidate)
        {
            InvalidateSession(session, "session_limit");
        }

        _context.RefreshTokens.UpdateRange(sessionsToInvalidate);
        await _context.SaveChangesAsync();
    }

    private static int NormalizeAccessTokenLifetimeMinutes(int value)
    {
        return Math.Clamp(value, MinAccessTokenLifetimeMinutes, MaxAccessTokenLifetimeMinutes);
    }

    private static int NormalizeRefreshTokenLifetimeDays(int value)
    {
        return Math.Clamp(value, MinRefreshTokenLifetimeDays, MaxRefreshTokenLifetimeDays);
    }

    private static int? NormalizeOptionalMaxActiveSessions(int? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Math.Clamp(value.Value, MinMaxActiveSessions, MaxMaxActiveSessions);
    }

    private void InvalidateSession(RefreshToken refreshToken, string reason)
    {
        refreshToken.Invalidated = true;
        refreshToken.InvalidatedAtUtc = DateTime.UtcNow;
        refreshToken.RevocationReason = TrimOrNull(reason, 200);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<RefreshToken?> FindStoredRefreshTokenAsync(string refreshToken)
    {
        var normalizedRefreshToken = NormalizeRefreshTokenInput(refreshToken);
        if (normalizedRefreshToken == null)
        {
            return null;
        }

        var hashedRefreshToken = HashRefreshToken(normalizedRefreshToken);
        var storedRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == hashedRefreshToken);
        if (storedRefreshToken != null)
        {
            return storedRefreshToken;
        }

        // Legacy fallback for previously persisted raw refresh tokens.
        return await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == normalizedRefreshToken);
    }

    private static string? NormalizeRefreshTokenInput(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        return refreshToken.Trim();
    }

    private async Task HandlePotentialRefreshTokenReplayAsync(RefreshToken refreshToken)
    {
        if (!refreshToken.Used &&
            !string.Equals(refreshToken.RevocationReason, "rotated", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await RevokeUserRefreshTokensAsync(refreshToken.UserId);

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user != null)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
    }

    private string? GetCurrentIpAddress()
    {
        return RequestContextInfoResolver.ResolveClientIpAddress(_httpContextAccessor.HttpContext);
    }

    private string? GetCurrentUserAgent()
    {
        return RequestContextInfoResolver.ResolveUserAgent(_httpContextAccessor.HttpContext);
    }
}
