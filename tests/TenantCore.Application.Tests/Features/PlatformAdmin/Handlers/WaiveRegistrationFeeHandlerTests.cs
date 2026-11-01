using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Handlers;

/// <summary>
/// The domain owns the waiver rule: a genuinely paid fee cannot be waived, and a second waiver is
/// refused rather than silently accepted. This only proves the handler reaches that rule and saves
/// when the waiver is legitimate.
/// </summary>
public class WaiveRegistrationFeeHandlerTests
{
    private readonly Mock<IClinicAccountRepository> _accounts = new();

    public WaiveRegistrationFeeHandlerTests()
    {
        _accounts.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private WaiveRegistrationFeeHandler CreateHandler() =>
        new(_accounts.Object, NullLogger<WaiveRegistrationFeeHandler>.Instance);

    private ClinicAccount GivenAccount()
    {
        var account = ClinicAccount.CreateDefault(Guid.NewGuid());
        _accounts.Setup(r => r.GetOrCreateAsync(account.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        return account;
    }

    private static WaiveRegistrationFeeCommand Command(Guid applicationId) =>
        new(applicationId, "Startup partner", Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_UnpaidFee_WaivesItAndSaves()
    {
        var account = GivenAccount();

        await CreateHandler().Handle(Command(account.ApplicationId), CancellationToken.None);

        account.RegistrationFeeWaived.Should().BeTrue();
        account.RegistrationFeeWaiveReason.Should().Be("Startup partner");
        account.RegistrationFeeWaivedByAdminEmail.Should().Be("admin@example.test");
        account.HasRegistrationFeeDue.Should().BeFalse();
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FeeAlreadyPaid_ThrowsAndDoesNotSave()
    {
        var account = GivenAccount();
        account.MarkRegistrationFeeCharged("payment:seed", DateTime.UtcNow);

        var action = () => CreateHandler().Handle(Command(account.ApplicationId), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("refund it instead of waiving it");
        account.RegistrationFeeWaived.Should().BeFalse();
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SecondWaiver_ThrowsAndDoesNotSave()
    {
        var account = GivenAccount();
        account.WaiveRegistrationFee("First waiver", "admin1@example.test");

        var action = () => CreateHandler().Handle(Command(account.ApplicationId), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("already been waived");
        account.RegistrationFeeWaiveReason.Should().Be("First waiver");
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClinicWithNoAccountRow_StillGetsAWaiverRow()
    {
        var account = GivenAccount();

        await CreateHandler().Handle(Command(account.ApplicationId), CancellationToken.None);

        _accounts.Verify(r => r.GetOrCreateAsync(account.ApplicationId, It.IsAny<CancellationToken>()), Times.Once,
            "the row is the only place this decision can live");
    }

    [Fact]
    public void Command_NamesTheActionAndAuditsTheClinicOnly()
    {
        var command = Command(Guid.NewGuid());

        command.ActionName.Should().Be("Clinic Registration Fee Waived");
        command.ActionLogContext.Should().NotContain("Startup partner",
            "a free-text reason is not part of the log context");
    }
}
