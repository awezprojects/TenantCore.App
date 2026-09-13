using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Medicines.Handlers;
using TenantCore.Application.Features.Medicines.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Medicines.Handlers;

public class GetMedicinesHandlerTests
{
    private readonly Mock<IMedicineRepository> _repository = new();

    [Fact]
    public async Task Handle_PassesRequestingClinicApplicationId_ToRepository()
    {
        var applicationId = Guid.NewGuid();
        var query = new GetMedicinesQuery(1, 20, null, null, null, null, null, null, applicationId);

        _repository.Setup(r => r.GetPagedAsync(
                query.Page, query.PageSize, query.Search, query.BrandName, query.GenericName,
                query.MedicineTypeId, query.DosageFormId, query.IsGeneric, applicationId,
                false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<Medicine>(), 0));

        var handler = new GetMedicinesHandler(_repository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        _repository.Verify(r => r.GetPagedAsync(
            query.Page, query.PageSize, query.Search, query.BrandName, query.GenericName,
            query.MedicineTypeId, query.DosageFormId, query.IsGeneric, applicationId,
            false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
