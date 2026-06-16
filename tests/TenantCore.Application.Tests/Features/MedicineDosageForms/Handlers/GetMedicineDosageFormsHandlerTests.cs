using FluentAssertions;
using Moq;
using TenantCore.Application.Features.MedicineDosageForms.Handlers;
using TenantCore.Application.Features.MedicineDosageForms.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Handlers;

public class GetMedicineDosageFormsHandlerTests
{
    private readonly Mock<IMedicineDosageFormRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsPagedResult()
    {
        var items = new[]
        {
            MedicineDosageForm.Create("Tablet", "Oral solid dosage form"),
            MedicineDosageForm.Create("Capsule", "Encapsulated dosage form")
        };
        _repository.Setup(r => r.GetPagedAsync(1, 10, "tab", It.IsAny<CancellationToken>()))
            .ReturnsAsync((items.AsEnumerable(), items.Length));

        var handler = new GetMedicineDosageFormsHandler(_repository.Object);
        var query = new GetMedicineDosageFormsQuery(1, 10, "tab");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(items[0].Id);
        result.Items[0].Name.Should().Be(items[0].Name);
        result.Items[1].Id.Should().Be(items[1].Id);
        result.Items[1].Name.Should().Be(items[1].Name);
        result.TotalCount.Should().Be(items.Length);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        _repository.Verify(r => r.GetPagedAsync(1, 10, "tab", It.IsAny<CancellationToken>()), Times.Once);
    }
}
