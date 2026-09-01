using FluentAssertions;
using Moq;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.ExpenseRecords.Handlers;

public class CreateExpenseRecordHandlerTests
{
    private readonly Mock<IExpenseRecordRepository> _repository = new();
    private readonly Mock<IExpenseCategoryRepository> _categoryRepository = new();
    private readonly Mock<ICounterSessionRepository> _sessionRepository = new();

    private CreateExpenseRecordHandler CreateHandler() =>
        new(_repository.Object, _categoryRepository.Object, _sessionRepository.Object);

    private void SetupActiveSession(Guid appId)
    {
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        _sessionRepository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesRecordAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = ExpenseCategory.Create(appId, "Transport", null);
        var recordedBy = Guid.NewGuid();

        SetupActiveSession(appId);
        _categoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _repository.Setup(r => r.AddAsync(It.IsAny<ExpenseRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = categoryId, Amount = 200, Notes = "Taxi" },
            recordedBy, appId);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.IsAny<ExpenseRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoActiveSession_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        _sessionRepository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterSession?)null);

        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = Guid.NewGuid(), Amount = 200 },
            Guid.NewGuid(), appId);

        var action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*counter session*");
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        SetupActiveSession(appId);
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseCategory?)null);

        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = Guid.NewGuid(), Amount = 200 },
            Guid.NewGuid(), appId);

        var action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryInactive_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var category = ExpenseCategory.Create(appId, "Transport", null);
        category.Update("Transport", null, false);

        SetupActiveSession(appId);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = category.Id, Amount = 200 },
            Guid.NewGuid(), appId);

        var action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
