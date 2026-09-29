using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TenantCore.Infrastructure.Services;

namespace TenantCore.Infrastructure.Tests.Services;

/// <summary>
/// A missing Email:Host/Email:From must fail loudly. It used to return silently, which made
/// every caller (SendEmailTaskHandler, SubmitPrescriptionHandler) record "email sent"
/// successfully even though nothing was ever delivered.
/// </summary>
public class EmailServiceTests
{
    private static EmailService CreateService(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new EmailService(configuration, NullLogger<EmailService>.Instance);
    }

    [Fact]
    public async Task SendAsync_WhenHostMissing_ThrowsInsteadOfSilentlySucceeding()
    {
        var service = CreateService(new Dictionary<string, string?> { ["Email:From"] = "noreply@example.com" });

        var act = async () => await service.SendAsync("patient@example.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendAsync_WhenFromMissing_ThrowsInsteadOfSilentlySucceeding()
    {
        var service = CreateService(new Dictionary<string, string?> { ["Email:Host"] = "smtp.example.com" });

        var act = async () => await service.SendAsync("patient@example.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
