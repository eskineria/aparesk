using Eskineria.Core.Compliance.Entities;
using Eskineria.Core.Shared.Localization;
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

        var now = DateTime.UtcNow;

        _dbContext.Set<TermsAndConditions>().AddRange(
            new TermsAndConditions
            {
                Id = Guid.NewGuid(),
                Type = "TermsOfService",
                Version = "1.0",
                Content = new LocalizedContent 
                { 
                    ["en-US"] = "<h1>Terms of Service</h1><p>Please accept our terms to continue.</p>",
                    ["tr-TR"] = "<h1>Kullanım Şartları</h1><p>Devam etmek için lütfen şartlarımızı kabul edin.</p>"
                },
                Summary = new LocalizedContent 
                { 
                    ["en-US"] = "Initial Terms of Service",
                    ["tr-TR"] = "Başlangıç Kullanım Şartları"
                },
                EffectiveDate = now,
                CreatedAt = now,
                IsActive = true
            },
            new TermsAndConditions
            {
                Id = Guid.NewGuid(),
                Type = "PrivacyPolicy",
                Version = "1.0",
                Content = new LocalizedContent 
                { 
                    ["en-US"] = "<h1>Privacy Policy</h1><p>We value your privacy.</p>",
                    ["tr-TR"] = "<h1>Gizlilik Politikası</h1><p>Gizliliğinize önem veriyoruz.</p>"
                },
                Summary = new LocalizedContent 
                { 
                    ["en-US"] = "Initial Privacy Policy",
                    ["tr-TR"] = "Başlangıç Gizlilik Politikası"
                },
                EffectiveDate = now,
                CreatedAt = now,
                IsActive = true
            }
        );

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Seeded initial Terms and Conditions");
    }
}
