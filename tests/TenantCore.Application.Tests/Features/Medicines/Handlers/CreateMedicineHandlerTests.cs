using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.Medicines.Commands;
using TenantCore.Application.Features.Medicines.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Medicines.Handlers;

public class CreateMedicineHandlerTests
{
    private readonly Mock<IMedicineRepository> _repository = new();
    private readonly Mock<ILogger<CreateMedicineHandler>> _logger = new();

    [Fact]
    public async Task Handle_WhenNoConflicts_CreatesMedicineScopedToRequestingClinic()
    {
        var applicationId = Guid.NewGuid();
        var command = new CreateMedicineCommand(
            "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, applicationId);

        _repository.Setup(r => r.FindSimilarAsync(
                command.Name, command.BrandName, command.Dosage, applicationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Medicine? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Medicine>(), It.IsAny<CancellationToken>()))
            .Callback<Medicine, CancellationToken>((m, _) => added = m)
            .Returns(Task.CompletedTask);

        var handler = new CreateMedicineHandler(_repository.Object, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be(command.Name);
        added.Should().NotBeNull();
        added!.ApplicationId.Should().Be(applicationId);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSimilarMedicineExists_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var command = new CreateMedicineCommand(
            "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, applicationId);

        var existing = Medicine.Create(
            "Paracetamol", null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, null);

        _repository.Setup(r => r.FindSimilarAsync(
                command.Name, command.BrandName, command.Dosage, applicationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var handler = new CreateMedicineHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Medicine>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
