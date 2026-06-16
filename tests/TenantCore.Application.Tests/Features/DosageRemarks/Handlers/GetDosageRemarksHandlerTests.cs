using FluentAssertions;
using Moq;
using TenantCore.Application.Features.DosageRemarks.Handlers;
using TenantCore.Application.Features.DosageRemarks.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Handlers;

public class GetDosageRemarksHandlerTests
{
    private readonly Mock<IDosageRemarkRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsPagedResult()
    {
        var applicationId = Guid.NewGuid();
        var items = new[]
        {
            DosageRemark.Create(applicationId, MedicineFormType.Tab, "Take after food", null, null),
            DosageRemark.Create(applicationId, MedicineFormType.Capsule, "Take before food", "भोजन से पहले लें", null)
        };
        _repository.Setup(r => r.GetPagedAsync(applicationId, 2, 25, MedicineFormType.Tab, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { items[0] }.AsEnumerable(), 1));

        var handler = new GetDosageRemarksHandler(_repository.Object);
        var query = new GetDosageRemarksQuery(applicationId, 2, 25, MedicineFormType.Tab);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(items[0].Id);
        result.Items[0].RemarkEnglish.Should().Be(items[0].RemarkEnglish);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(25);
        _repository.Verify(r => r.GetPagedAsync(applicationId, 2, 25, MedicineFormType.Tab, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPageSizeExceedsMax_ClampsTo100()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetPagedAsync(applicationId, 1, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<DosageRemark>(), 0));

        var handler = new GetDosageRemarksHandler(_repository.Object);
        var query = new GetDosageRemarksQuery(applicationId, 1, 250, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.PageSize.Should().Be(100);
        _repository.Verify(r => r.GetPagedAsync(applicationId, 1, 100, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
