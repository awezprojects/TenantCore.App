using FluentAssertions;
using Moq;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Handlers;

public class CreateMedicineDosageFormHandlerTests
{
    private readonly Mock<IMedicineDosageFormRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenNameIsUnique_CreatesFormAndReturnsDto()
    {
        var command = new CreateMedicineDosageFormCommand("Tablet", "Oral solid dosage form");
        MedicineDosageForm? captured = null;
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineDosageForm?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<MedicineDosageForm>(), It.IsAny<CancellationToken>()))
            .Callback<MedicineDosageForm, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateMedicineDosageFormHandler(_repository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Name.Should().Be(command.Name);
        captured.Description.Should().Be(command.Description);
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineDosageForm>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsInvalidOperationException()
    {
        var command = new CreateMedicineDosageFormCommand("Tablet", "Oral solid dosage form");
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MedicineDosageForm.Create(command.Name, command.Description));

        var handler = new CreateMedicineDosageFormHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Dosage form '{command.Name}' already exists.");
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineDosageForm>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveFails_PropagatesException()
    {
        var command = new CreateMedicineDosageFormCommand("Tablet", "Oral solid dosage form");
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineDosageForm?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<MedicineDosageForm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var handler = new CreateMedicineDosageFormHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineDosageForm>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
