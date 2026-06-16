using FluentAssertions;
using Moq;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Handlers;

public class DeleteMedicineDosageFormHandlerTests
{
    private readonly Mock<IMedicineDosageFormRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_DeactivatesForm()
    {
        var form = MedicineDosageForm.Create("Tablet", "Oral solid dosage form");
        _repository.Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteMedicineDosageFormHandler(_repository.Object);
        var command = new DeleteMedicineDosageFormCommand(form.Id);

        await handler.Handle(command, CancellationToken.None);

        form.IsActive.Should().BeFalse();
        _repository.Verify(r => r.Update(form), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteMedicineDosageFormCommand(Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineDosageForm?)null);

        var handler = new DeleteMedicineDosageFormHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'MedicineDosageForm' with key '{command.Id}' was not found.");
        _repository.Verify(r => r.Update(It.IsAny<MedicineDosageForm>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
