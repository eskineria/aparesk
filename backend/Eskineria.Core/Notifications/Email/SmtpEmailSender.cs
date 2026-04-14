using Eskineria.Core.Notifications.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Eskineria.Core.Notifications.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Eskineria.Core.Notifications.Email;

public class SmtpEmailSender : IEmailSender
{
    private const X509ChainStatusFlags AllowedDevelopmentRevocationFlags =
        X509ChainStatusFlags.NoError |
        X509ChainStatusFlags.RevocationStatusUnknown |
        X509ChainStatusFlags.OfflineRevocation;

    private readonly EmailOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        IHostEnvironment environment,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.FromName))
        {
            _options.FromName = _options.FromAddress;
        }
        
        ValidateConfiguration();
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedTo = NotificationSecurity.NormalizeAndValidateEmail(to, nameof(to));
        var normalizedSubject = NotificationSecurity.NormalizeSubject(subject, nameof(subject));
        var normalizedBody = NotificationSecurity.NormalizeBody(body, nameof(body));

        try
        {
            var message = CreateMessage(normalizedTo, normalizedSubject, normalizedBody, isHtml);
            await SendMessageAsync(message, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {Recipient}", NotificationSecurity.MaskEmailForLog(normalizedTo));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email sending cancelled for {Recipient}", NotificationSecurity.MaskEmailForLog(normalizedTo));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", NotificationSecurity.MaskEmailForLog(normalizedTo));
            throw;
        }
    }

    private MimeMessage CreateMessage(string to, string subject, string body, bool isHtml)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = isHtml ? body : null,
            TextBody = !isHtml ? body : null
        };

        message.Body = builder.ToMessageBody();
        return message;
    }

    private async Task SendMessageAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = ValidateServerCertificate;
        
        try
        {
            var secureSocketOptions = _options.EnableSsl 
                ? SecureSocketOptions.Auto 
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_options.SmtpUser))
            {
                await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPass, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (!_environment.IsDevelopment())
        {
            return false;
        }

        if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0 || chain is null)
        {
            return false;
        }

        var hasOnlyRevocationIssues = chain.ChainStatus.All(status => (status.Status & ~AllowedDevelopmentRevocationFlags) == 0);
        if (!hasOnlyRevocationIssues)
        {
            return false;
        }

        _logger.LogWarning(
            "Accepting SMTP certificate in Development because only revocation checks failed for host {SmtpHost}.",
            _options.SmtpHost);

        return true;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            throw new InvalidOperationException("SMTP host is not configured");

        if (_options.SmtpPort <= 0 || _options.SmtpPort > 65535)
            throw new InvalidOperationException($"Invalid SMTP port: {_options.SmtpPort}");

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException("From email address is not configured");

        if (!string.IsNullOrWhiteSpace(_options.SmtpUser) && string.IsNullOrWhiteSpace(_options.SmtpPass))
            throw new InvalidOperationException("SMTP password is required when SMTP user is configured");
    }
}
