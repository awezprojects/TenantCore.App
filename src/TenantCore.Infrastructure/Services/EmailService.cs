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
            // Must throw rather than silently return: callers (SendEmailTaskHandler,
            // SubmitPrescriptionHandler) treat a normal return as "email sent" and record
            // success accordingly — a silent no-op here previously made every send look
            // successful in the workflow task status and the Admin portal even though no
            // email was ever delivered.
            logger.LogError("Email not configured (Email:Host/Email:From missing). Cannot send to {To}", to);
            throw new InvalidOperationException(
                "Email is not configured (Email:Host / Email:From missing in configuration).");
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
