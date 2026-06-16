using FluentAssertions;
using Moq;
using TenantCore.Application.Features.MedicineTypes.Handlers;
using TenantCore.Application.Features.MedicineTypes.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Handlers;

public class GetMedicineTypeByIdHandlerTests
{
    private readonly Mock<IMedicineTypeRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_ReturnsMappedDto()
    {
        var medicineType = MedicineType.Create("Tablet", "Oral solid dosage form");
        var query = new GetMedicineTypeByIdQuery(medicineType.Id);

        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicineType);

        var handler = new GetMedicineTypeByIdHandler(_repository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(medicineType.Id);
        result.Name.Should().Be(medicineType.Name);
        result.Description.Should().Be(medicineType.Description);
        result.IsActive.Should().Be(medicineType.IsActive);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var query = new GetMedicineTypeByIdQuery(Guid.NewGuid());

        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineType?)null);

        var handler = new GetMedicineTypeByIdHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
