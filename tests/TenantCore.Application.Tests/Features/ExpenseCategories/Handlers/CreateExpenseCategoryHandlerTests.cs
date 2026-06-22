using FluentAssertions;
using Moq;
using TenantCore.Application.Features.ExpenseCategories.Commands;
using TenantCore.Application.Features.ExpenseCategories.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.ExpenseCategories.Handlers;

public class CreateExpenseCategoryHandlerTests
{
    private readonly Mock<IExpenseCategoryRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenValid_CreatesCategoryAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var command = new CreateExpenseCategoryCommand(
            new CreateExpenseCategoryRequest { Name = "Transport", Description = "Travel costs" }, appId);

        _repository.Setup(r => r.AddAsync(It.IsAny<ExpenseCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateExpenseCategoryHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.IsAny<ExpenseCategory>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
