using FluentAssertions;
using Moq;
using TenantCore.Application.Features.MedicineTypes.Handlers;
using TenantCore.Application.Features.MedicineTypes.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Handlers;

public class GetMedicineTypesHandlerTests
{
    private readonly Mock<IMedicineTypeRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsPagedResult()
    {
        var query = new GetMedicineTypesQuery(Page: 2, PageSize: 5, Search: "tab");
        var medicineTypes = new[]
        {
            MedicineType.Create("Tablet", "Oral solid dosage form"),
            MedicineType.Create("Capsule", "Encapsulated dosage form")
        };

        _repository.Setup(r => r.GetPagedAsync(query.Page, query.PageSize, query.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync((medicineTypes, 7));

        var handler = new GetMedicineTypesHandler(_repository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(7);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Name).Should().BeEquivalentTo(new[] { "Tablet", "Capsule" });
    }
}
