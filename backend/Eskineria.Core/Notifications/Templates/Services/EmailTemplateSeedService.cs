using Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Eskineria.Core.Notifications.Templates.Services;

public sealed class EmailTemplateSeedService
{
    private readonly DbContext _dbContext;
    private readonly IHostEnvironment _environment;

    private sealed record SeedTemplateDefinition(
        string Key,
        string Culture,
        string Name,
        string Subject,
        string FileName);

    public EmailTemplateSeedService(DbContext dbContext, IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task SeedStartupAsync()
    {
        var defaultEmailTemplates = GetDefaultEmailTemplates();

        var existingTemplateKeys = await _dbContext.Set<EmailTemplate>()
            .AsNoTracking()
            .Select(x => new { x.Key, x.Culture })
            .ToListAsync();

        var existingTemplateKeySet = existingTemplateKeys
            .Select(x => $"{x.Key}::{x.Culture}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var subjectPlaceholderByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VerifyEmailCode"] = "{{ VerificationCodeEmailTitle }}",
            ["ResetPassword"] = "{{ ResetPasswordEmailTitle }}",
            ["Welcome"] = "{{ WelcomeEmailTitle }}",
            ["PasswordChangedAlert"] = "{{ PasswordChangedAlertEmailTitle }}",
            ["LoginAlert"] = "{{ LoginAlertEmailTitle }}",
            ["MfaActionCode"] = "{{ MfaActionEmailSubject }}",
            ["MfaLoginCode"] = "{{ MfaVerificationEmailTitle }}",
            ["AccountLocked"] = "{{ AccountLockedEmailTitle }}",
            ["ComplianceReacceptance"] = "{{ ComplianceReacceptanceEmailTitle }}"
        };

        var legacySubjectsByKey = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["VerifyEmailCode"] = new(StringComparer.OrdinalIgnoreCase) { "Your verification code", "Your Eskineria verification code", "Eskineria doğrulama kodunuz" },
            ["ResetPassword"] = new(StringComparer.OrdinalIgnoreCase) { "Password Reset Request", "Şifre Sıfırlama İsteği" },
            ["Welcome"] = new(StringComparer.OrdinalIgnoreCase) { "Welcome to Eskineria!", "Eskineria'ya Hoş Geldiniz!", "Eskineria'ya hoş geldiniz!" },
            ["PasswordChangedAlert"] = new(StringComparer.OrdinalIgnoreCase) { "Your password was changed", "Şifreniz değiştirildi" },
            ["LoginAlert"] = new(StringComparer.OrdinalIgnoreCase) { "New login detected", "Yeni giriş tespit edildi" },
            ["MfaActionCode"] = new(StringComparer.OrdinalIgnoreCase) { "MFA Action Verification Code", "MFA İşlem Doğrulama Kodu" },
            ["MfaLoginCode"] = new(StringComparer.OrdinalIgnoreCase) { "Your MFA verification code", "MFA doğrulama kodunuz" },
            ["AccountLocked"] = new(StringComparer.OrdinalIgnoreCase) { "Account Locked", "Hesabınız Kilitlendi" },
            ["ComplianceReacceptance"] = new(StringComparer.OrdinalIgnoreCase) { "Compliance Agreement Update", "Uyum Sözleşmesi Güncellemesi" }
        };

        var defaultTemplateKeys = subjectPlaceholderByKey.Keys.ToList();
        var templatesToSync = await _dbContext.Set<EmailTemplate>()
            .Where(x => defaultTemplateKeys.Contains(x.Key))
            .ToListAsync();

        var syncedTemplateCount = 0;
        var syncTimestamp = DateTime.UtcNow;

        foreach (var template in templatesToSync)
        {
            var changed = false;

            if (subjectPlaceholderByKey.TryGetValue(template.Key, out var subjectPlaceholder))
            {
                var currentSubject = template.Subject?.Trim() ?? string.Empty;
                if (!string.Equals(currentSubject, subjectPlaceholder, StringComparison.Ordinal) &&
                    legacySubjectsByKey.TryGetValue(template.Key, out var legacySubjects) &&
                    legacySubjects.Contains(currentSubject))
                {
                    template.Subject = subjectPlaceholder;
                    changed = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(template.Body))
            {
                var updatedBody = template.Body
                    .Replace(">Review Security<", ">{{ ReviewSecurityButton }}<", StringComparison.Ordinal)
                    .Replace(">Account Security<", ">{{ AccountSecurityButton }}<", StringComparison.Ordinal);

                if (!string.Equals(template.Body, updatedBody, StringComparison.Ordinal))
                {
                    template.Body = updatedBody;
                    changed = true;
                }
            }

            if (!changed)
            {
                continue;
            }

            template.CurrentVersion += 1;
            template.IsDraft = false;
            template.PublishedVersion = template.CurrentVersion;
            template.PublishedAt = syncTimestamp;
            template.UpdatedAt = syncTimestamp;

            _dbContext.Set<EmailTemplateRevision>().Add(new EmailTemplateRevision
            {
                EmailTemplateId = template.Id,
                Version = template.CurrentVersion,
                Name = template.Name,
                Subject = template.Subject ?? string.Empty,
                Body = template.Body ?? string.Empty,
                RequiredVariables = template.RequiredVariables ?? "[]",
                IsPublishedSnapshot = true,
                ChangeSource = "StartupSeed",
                CreatedAt = syncTimestamp
            });

            syncedTemplateCount++;
        }

        if (syncedTemplateCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"[StartupSeed] Synced {syncedTemplateCount} default email template(s) for localization placeholders.");
        }

        var templatesToInsert = new List<EmailTemplate>();
        foreach (var template in defaultEmailTemplates)
        {
            if (existingTemplateKeySet.Contains($"{template.Key}::{template.Culture}"))
            {
                continue;
            }

            var templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", template.FileName);
            if (!File.Exists(templatePath))
            {
                continue;
            }

            var body = await File.ReadAllTextAsync(templatePath);
            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            var now = DateTime.UtcNow;
            templatesToInsert.Add(new EmailTemplate
            {
                Key = template.Key,
                Culture = template.Culture,
                Name = template.Name,
                Subject = template.Subject,
                Body = body,
                RequiredVariables = "[]",
                IsActive = true,
                IsDraft = false,
                CurrentVersion = 1,
                PublishedVersion = 1,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (templatesToInsert.Count == 0)
        {
            return;
        }

        await _dbContext.Set<EmailTemplate>().AddRangeAsync(templatesToInsert);
        await _dbContext.SaveChangesAsync();

        var revisionsToInsert = templatesToInsert.Select(template => new EmailTemplateRevision
        {
            EmailTemplateId = template.Id,
            Version = template.CurrentVersion,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            RequiredVariables = template.RequiredVariables,
            IsPublishedSnapshot = true,
            ChangeSource = "StartupSeed",
            CreatedAt = template.CreatedAt
        }).ToList();

        await _dbContext.Set<EmailTemplateRevision>().AddRangeAsync(revisionsToInsert);
        await _dbContext.SaveChangesAsync();

        Console.WriteLine($"[StartupSeed] Seeded {templatesToInsert.Count} email template(s).");
    }

    private static SeedTemplateDefinition[] GetDefaultEmailTemplates()
    {
        return
        [
            new("VerifyEmailCode", "en-US", "Email Verification Code", "{{ VerificationCodeEmailTitle }}", "VerifyEmailCode.sbn"),
            new("ResetPassword", "en-US", "Reset Password", "{{ ResetPasswordEmailTitle }}", "ResetPassword.sbn"),
            new("Welcome", "en-US", "Welcome Email", "{{ WelcomeEmailTitle }}", "Welcome.sbn"),
            new("PasswordChangedAlert", "en-US", "Password Changed Alert", "{{ PasswordChangedAlertEmailTitle }}", "PasswordChangedAlert.sbn"),
            new("LoginAlert", "en-US", "Login Alert", "{{ LoginAlertEmailTitle }}", "LoginAlert.sbn"),
            new("MfaActionCode", "en-US", "MFA Action Code", "{{ MfaActionEmailSubject }}", "MfaActionCode.sbn"),
            new("MfaLoginCode", "en-US", "MFA Login Code", "{{ MfaVerificationEmailTitle }}", "MfaLoginCode.sbn"),
            new("AccountLocked", "en-US", "Account Locked", "{{ AccountLockedEmailTitle }}", "AccountLocked.sbn"),
            new("VerifyEmailCode", "tr-TR", "E-posta Doğrulama Kodu", "{{ VerificationCodeEmailTitle }}", "VerifyEmailCode.sbn"),
            new("ResetPassword", "tr-TR", "Şifre Sıfırlama", "{{ ResetPasswordEmailTitle }}", "ResetPassword.sbn"),
            new("Welcome", "tr-TR", "Hoş Geldiniz E-postası", "{{ WelcomeEmailTitle }}", "Welcome.sbn"),
            new("PasswordChangedAlert", "tr-TR", "Şifre Değişikliği Uyarısı", "{{ PasswordChangedAlertEmailTitle }}", "PasswordChangedAlert.sbn"),
            new("LoginAlert", "tr-TR", "Giriş Uyarısı", "{{ LoginAlertEmailTitle }}", "LoginAlert.sbn"),
            new("MfaActionCode", "tr-TR", "MFA İşlem Kodu", "{{ MfaActionEmailSubject }}", "MfaActionCode.sbn"),
            new("MfaLoginCode", "tr-TR", "MFA Giriş Kodu", "{{ MfaVerificationEmailTitle }}", "MfaLoginCode.sbn"),
            new("AccountLocked", "tr-TR", "Hesap Kilitlendi", "{{ AccountLockedEmailTitle }}", "AccountLocked.sbn"),
            new("ComplianceReacceptance", "en-US", "Compliance Update", "{{ ComplianceReacceptanceEmailTitle }}", "ComplianceReacceptance.sbn"),
            new("ComplianceReacceptance", "tr-TR", "Uyum Güncellemesi", "{{ ComplianceReacceptanceEmailTitle }}", "ComplianceReacceptance.sbn")
        ];
    }
}
