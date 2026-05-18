using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        byte[]? attachmentBytes = null,
        string? attachmentName = null,
        CancellationToken ct = default)
    {
        var host = configuration["Email:Host"];
        var port = int.Parse(configuration["Email:Port"] ?? "587");
        var from = configuration["Email:From"] ?? string.Empty;
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
        {
            logger.LogWarning("Email not configured. Skipping send to {To}", to);
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };

        if (attachmentBytes is not null && attachmentName is not null)
        {
            var stream = new MemoryStream(attachmentBytes);
            message.Attachments.Add(new Attachment(stream, attachmentName, "application/pdf"));
        }

        await client.SendMailAsync(message, ct);
        logger.LogInformation("Prescription email sent to {To}", to);
    }
}
