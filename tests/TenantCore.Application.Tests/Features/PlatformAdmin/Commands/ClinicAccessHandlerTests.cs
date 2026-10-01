using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class ClinicAccessHandlerTests
{
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();

    public ClinicAccessHandlerTests()
        => _accountRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private SuspendClinicHandler CreateSuspendHandler()
        => new(_accountRepository.Object, Mock.Of<ILogger<SuspendClinicHandler>>());

    private ReactivateClinicHandler CreateReactivateHandler()
        => new(_accountRepository.Object, Mock.Of<ILogger<ReactivateClinicHandler>>());

    private SuspendClinicCommand SuspendCommand(string message = "Payment overdue")
        => new(_applicationId, message, _adminUserId, "admin@example.test");

    [Fact]
    public async Task Handle_Suspend_NoAccountRow_CreatesOneAndSuspends()
    {
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);

        ClinicAccount? added = null;
        _accountRepository.Setup(r => r.AddAsync(It.IsAny<ClinicAccount>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicAccount, CancellationToken>((a, _) => added = a);

        await CreateSuspendHandler().Handle(SuspendCommand(), CancellationToken.None);

        added.Should().NotBeNull();
        added!.ApplicationId.Should().Be(_applicationId);
        added.IsSuspended.Should().BeTrue();
        added.SuspensionMessage.Should().Be("Payment overdue");
        _accountRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Suspend_ExistingActiveAccount_SuspendsWithoutAdding()
    {
        var account = ClinicAccount.CreateDefault(_applicationId);
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        await CreateSuspendHandler().Handle(SuspendCommand(), CancellationToken.None);

        account.IsSuspended.Should().BeTrue();
        _accountRepository.Verify(r => r.AddAsync(It.IsAny<ClinicAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Suspend_AlreadySuspended_ThrowsInvalidOperationException()
    {
        var account = ClinicAccount.CreateDefault(_applicationId);
        account.Suspend("first", "admin@example.test");
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var act = async () => await CreateSuspendHandler().Handle(SuspendCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Reactivate_SuspendedAccount_RestoresAccess()
    {
        var account = ClinicAccount.CreateDefault(_applicationId);
        account.Suspend("overdue", "admin@example.test");
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        await CreateReactivateHandler().Handle(
            new ReactivateClinicCommand(_applicationId, _adminUserId, "admin@example.test"), CancellationToken.None);

        account.IsSuspended.Should().BeFalse();
        _accountRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Reactivate_NoAccountRow_ThrowsInvalidOperationException()
    {
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);

        var act = async () => await CreateReactivateHandler().Handle(
            new ReactivateClinicCommand(_applicationId, _adminUserId, "admin@example.test"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not suspended*");
    }

    [Fact]
    public void SuspendCommand_ActionLogContext_ContainsOnlyIdentifiers()
    {
        var command = SuspendCommand("Payment overdue — sensitive detail");

        // ADR-011: identifiers only. The admin's free-text message must never reach the log.
        command.ActionLogContext.Should().Contain($"applicationId={_applicationId}");
        command.ActionLogContext.Should().Contain($"adminUserId={_adminUserId}");
        command.ActionLogContext.Should().NotContain("sensitive detail");
    }
}
