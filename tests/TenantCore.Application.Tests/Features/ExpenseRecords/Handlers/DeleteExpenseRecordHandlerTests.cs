using FluentAssertions;
using Moq;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.ExpenseRecords.Handlers;

public class DeleteExpenseRecordHandlerTests
{
    private readonly Mock<IExpenseRecordRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_DeletesRecord()
    {
        var appId = Guid.NewGuid();
        var record = ExpenseRecord.Create(appId, Guid.NewGuid(), "Transport", 200, null, Guid.NewGuid(), null);

        _repository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _repository.Setup(r => r.Delete(record));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteExpenseRecordHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteExpenseRecordCommand(record.Id, appId), CancellationToken.None);

        await action.Should().NotThrowAsync();
        _repository.Verify(r => r.Delete(record), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseRecord?)null);

        var handler = new DeleteExpenseRecordHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteExpenseRecordCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCrossTenant_ThrowsNotFoundException()
    {
        var record = ExpenseRecord.Create(Guid.NewGuid(), Guid.NewGuid(), "Transport", 200, null, Guid.NewGuid(), null);

        _repository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var handler = new DeleteExpenseRecordHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteExpenseRecordCommand(record.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
