using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Handlers;
using TenantCore.Application.Tests.Common.Logging;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Handlers;

public class UpdateMedicineTypeHandlerTests
{
    private readonly Mock<IMedicineTypeRepository> _repository = new();
    private readonly Mock<ILogger<UpdateMedicineTypeHandler>> _logger = new();

    [Fact]
    public async Task Handle_WhenFoundAndNameIsUnique_UpdatesAndReturnsDto()
    {
        var medicineType = MedicineType.Create("Tablet", "Oral solid dosage form");
        var command = new UpdateMedicineTypeCommand(medicineType.Id, "Capsule", "Updated description", false);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicineType);
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineType?)null);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateMedicineTypeHandler(_repository.Object, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        medicineType.Name.Should().Be(command.Name);
        medicineType.Description.Should().Be(command.Description);
        medicineType.IsActive.Should().Be(command.IsActive);
        result.Id.Should().Be(medicineType.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().Be(command.IsActive);
        _repository.Verify(r => r.Update(medicineType), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.VerifyLog(LogLevel.Information, "Updating medicine type", Times.Once());
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateMedicineTypeCommand(Guid.NewGuid(), "Capsule", "Updated description", false);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineType?)null);

        var handler = new UpdateMedicineTypeHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyTakenByOther_ThrowsInvalidOperationException()
    {
        var medicineType = MedicineType.Create("Tablet", "Oral solid dosage form");
        var duplicate = MedicineType.Create("Capsule", "Existing duplicate");
        var command = new UpdateMedicineTypeCommand(medicineType.Id, duplicate.Name, "Updated description", true);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicineType);
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var handler = new UpdateMedicineTypeHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"A medicine type with the name '{command.Name}' already exists.");
        _repository.Verify(r => r.Update(It.IsAny<MedicineType>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSameNameSameId_UpdatesSuccessfully()
    {
        var medicineType = MedicineType.Create("Tablet", "Oral solid dosage form");
        var duplicateWithSameId = MedicineType.Create(medicineType.Name, "Another description");
        SetId(duplicateWithSameId, medicineType.Id);
        var command = new UpdateMedicineTypeCommand(medicineType.Id, medicineType.Name, "Updated description", false);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicineType);
        _repository.Setup(r => r.GetByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateWithSameId);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateMedicineTypeHandler(_repository.Object, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().BeFalse();
        _repository.Verify(r => r.Update(medicineType), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void SetId(MedicineType medicineType, Guid id)
    {
        typeof(MedicineType)
            .BaseType!
            .GetProperty("Id")!
            .SetValue(medicineType, id);
    }
}
