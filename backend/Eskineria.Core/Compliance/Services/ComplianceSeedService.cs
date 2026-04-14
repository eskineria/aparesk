using Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eskineria.Core.Compliance.Services;

public sealed class ComplianceSeedService
{
    private readonly DbContext _dbContext;
    private readonly ILogger<ComplianceSeedService> _logger;

    public ComplianceSeedService(DbContext dbContext, ILogger<ComplianceSeedService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedInitialTermsAsync()
    {
        if (await _dbContext.Set<TermsAndConditions>().AnyAsync())
        {
            return;
        }

        _dbContext.Set<TermsAndConditions>().Add(new TermsAndConditions
        {
            Id = Guid.NewGuid(),
            Type = "TermsOfService",
            Version = "1.0",
            Content = "<h1>Terms of Service</h1><p>Please accept our terms to continue.</p>",
            Summary = "Initial Terms of Service",
            EffectiveDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Seeded initial Terms and Conditions");
    }
}
