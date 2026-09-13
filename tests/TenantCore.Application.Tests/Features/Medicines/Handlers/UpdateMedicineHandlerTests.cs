using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.Medicines.Commands;
using TenantCore.Application.Features.Medicines.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Medicines.Handlers;

public class UpdateMedicineHandlerTests
{
    private readonly Mock<IMedicineRepository> _repository = new();
    private readonly Mock<ILogger<UpdateMedicineHandler>> _logger = new();

    [Fact]
    public async Task Handle_WhenMedicineBelongsToRequestingClinic_UpdatesSuccessfully()
    {
        var applicationId = Guid.NewGuid();
        var medicine = Medicine.Create(
            "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, applicationId);

        var command = new UpdateMedicineCommand(
            medicine.Id, "Paracetamol Updated", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, true, null, applicationId);

        _repository.Setup(r => r.GetByIdWithTypeAsync(medicine.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicine);
        _repository.Setup(r => r.FindSimilarAsync(
                command.Name, command.BrandName, command.Dosage, applicationId, medicine.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateMedicineHandler(_repository.Object, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be(command.Name);
        _repository.Verify(r => r.Update(medicine), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMedicineNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateMedicineCommand(
            Guid.NewGuid(), "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, true, null, Guid.NewGuid());

        _repository.Setup(r => r.GetByIdWithTypeAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Medicine?)null);

        var handler = new UpdateMedicineHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMedicineIsSystemWide_ThrowsUnauthorizedAccessException()
    {
        var medicine = Medicine.Create(
            "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, applicationId: null);

        var command = new UpdateMedicineCommand(
            medicine.Id, "Paracetamol Updated", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, true, null, Guid.NewGuid());

        _repository.Setup(r => r.GetByIdWithTypeAsync(medicine.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicine);

        var handler = new UpdateMedicineHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        _repository.Verify(r => r.Update(It.IsAny<Medicine>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMedicineBelongsToAnotherClinic_ThrowsUnauthorizedAccessException()
    {
        var ownerApplicationId = Guid.NewGuid();
        var requestingApplicationId = Guid.NewGuid();
        var medicine = Medicine.Create(
            "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, ownerApplicationId);

        var command = new UpdateMedicineCommand(
            medicine.Id, "Paracetamol Updated", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, true, null, requestingApplicationId);

        _repository.Setup(r => r.GetByIdWithTypeAsync(medicine.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicine);

        var handler = new UpdateMedicineHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        _repository.Verify(r => r.Update(It.IsAny<Medicine>()), Times.Never);
    }
}
