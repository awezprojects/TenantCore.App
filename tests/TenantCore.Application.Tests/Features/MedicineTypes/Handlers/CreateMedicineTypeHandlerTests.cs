using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Handlers;
using TenantCore.Application.Tests.Common.Logging;
using TenantCore.Application.Tests.TestData;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Handlers;

public class CreateMedicineTypeHandlerTests
{
    private readonly Mock<IMedicineTypeRepository> _repository = new();
    private readonly Mock<ILogger<CreateMedicineTypeHandler>> _logger = new();

    [Fact]
    public async Task Handle_WhenNameIsUnique_CreatesMedicineTypeAndReturnsDto()
    {
        var command = ApplicationTestData.CreateMedicineTypeCommand();
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineType?)null);
        MedicineType? captured = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<MedicineType>(), It.IsAny<CancellationToken>()))
            .Callback<MedicineType, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateMedicineTypeHandler(_repository.Object, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Name.Should().Be(command.Name);
        captured.Description.Should().Be(command.Description);
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineType>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.VerifyLog(LogLevel.Information, "Creating medicine type", Times.Once());
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsInvalidOperationException()
    {
        var command = new CreateMedicineTypeCommand(ApplicationTestData.ExistingMedicineTypeName, ApplicationTestData.MedicineTypeDescription);
        var existingMedicineType = MedicineType.Create(command.Name, command.Description);
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMedicineType);
        var handler = new CreateMedicineTypeHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"A medicine type with the name '{command.Name}' already exists.");
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineType>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniquenessCheckFails_PropagatesException()
    {
        var command = ApplicationTestData.CreateMedicineTypeCommand();
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("lookup failed"));
        var handler = new CreateMedicineTypeHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("lookup failed");
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineType>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_PropagatesException()
    {
        var command = ApplicationTestData.CreateMedicineTypeCommand();
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineType?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<MedicineType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));
        var handler = new CreateMedicineTypeHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");
        _repository.Verify(r => r.AddAsync(It.IsAny<MedicineType>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
