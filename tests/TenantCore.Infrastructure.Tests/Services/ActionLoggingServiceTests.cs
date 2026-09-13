using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Infrastructure.Services;
using TenantCore.Logging;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Tests.Services;

public class ActionLoggingServiceTests
{
    private static (ActionLoggingService Service, Mock<IAppLogWriter> Writer) CreateService()
    {
        var writer = new Mock<IAppLogWriter>();
        var options = Options.Create(new AppLoggingOptions { ActionLogTable = "ActionLogs" });
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Development");

        var service = new ActionLoggingService(
            writer.Object, options, environment.Object, NullLogger<ActionLoggingService>.Instance);

        return (service, writer);
    }

    [Fact]
    public async Task LogStartedAsync_WritesEntryToActionLogTable_WithStatusStartedAndNoDuration()
    {
        var (service, writer) = CreateService();
        var correlationId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        LogEntry? captured = null;
        writer.Setup(w => w.WriteAsync("ActionLogs", It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((_, e, _) => captured = e)
            .Returns(Task.CompletedTask);

        await service.LogStartedAsync(correlationId, "Patient Registration", "RegisterPatientCommand", applicationId, userId);

        captured.Should().NotBeNull();
        captured!.Category.Should().Be(LogCategory.Action.ToString());
        captured.Source.Should().Be("Patient Registration");
        captured.Status.Should().Be("Started");
        captured.CorrelationId.Should().Be(correlationId.ToString());
        captured.ApplicationId.Should().Be(applicationId.ToString());
        captured.UserId.Should().Be(userId.ToString());
        captured.AdditionalContext.Should().Be("RegisterPatientCommand");
        captured.DurationMs.Should().BeNull();
    }

    [Fact]
    public async Task LogCompletedAsync_WritesEntry_WithStatusCompletedAndDuration()
    {
        var (service, writer) = CreateService();
        LogEntry? captured = null;
        writer.Setup(w => w.WriteAsync("ActionLogs", It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((_, e, _) => captured = e)
            .Returns(Task.CompletedTask);

        await service.LogCompletedAsync(Guid.NewGuid(), "OPD Registration", "CreateOpdRegistrationCommand", Guid.NewGuid(), null, 42);

        captured!.Status.Should().Be("Completed");
        captured.DurationMs.Should().Be(42);
        captured.UserId.Should().BeNull();
    }

    [Fact]
    public async Task LogFailedAsync_WritesEntry_WithStatusFailedAndErrorMessageAsMessage()
    {
        var (service, writer) = CreateService();
        LogEntry? captured = null;
        writer.Setup(w => w.WriteAsync("ActionLogs", It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((_, e, _) => captured = e)
            .Returns(Task.CompletedTask);

        await service.LogFailedAsync(Guid.NewGuid(), "OPD Refund", "ProcessOpdRefundCommand", Guid.NewGuid(), Guid.NewGuid(), 10, "No active counter session");

        captured!.Status.Should().Be("Failed");
        captured.Message.Should().Be("No active counter session");
        captured.DurationMs.Should().Be(10);
    }

    [Fact]
    public async Task LogStartedAsync_NullOrEmptyApplicationId_WritesNullApplicationId()
    {
        var (service, writer) = CreateService();
        LogEntry? captured = null;
        writer.Setup(w => w.WriteAsync("ActionLogs", It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((_, e, _) => captured = e)
            .Returns(Task.CompletedTask);

        await service.LogStartedAsync(Guid.NewGuid(), "Medicine Dosage Form Deletion", "DeleteMedicineDosageFormCommand", Guid.Empty, null);

        captured!.ApplicationId.Should().BeNull();
    }

    [Fact]
    public async Task LogStartedAsync_WriterThrows_SwallowsExceptionAndDoesNotPropagate()
    {
        var (service, writer) = CreateService();
        writer.Setup(w => w.WriteAsync(It.IsAny<string>(), It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage down"));

        Func<Task> act = () => service.LogStartedAsync(Guid.NewGuid(), "Patient Registration", "RegisterPatientCommand", Guid.NewGuid(), Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }
}
