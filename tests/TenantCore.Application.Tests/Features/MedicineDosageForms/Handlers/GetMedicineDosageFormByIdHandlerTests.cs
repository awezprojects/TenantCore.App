using FluentAssertions;
using Moq;
using TenantCore.Application.Features.MedicineDosageForms.Handlers;
using TenantCore.Application.Features.MedicineDosageForms.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Handlers;

public class GetMedicineDosageFormByIdHandlerTests
{
    private readonly Mock<IMedicineDosageFormRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_ReturnsMappedDto()
    {
        var form = MedicineDosageForm.Create("Tablet", "Oral solid dosage form");
        _repository.Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var handler = new GetMedicineDosageFormByIdHandler(_repository.Object);
        var query = new GetMedicineDosageFormByIdQuery(form.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(form.Id);
        result.Name.Should().Be(form.Name);
        result.Description.Should().Be(form.Description);
        result.IsActive.Should().Be(form.IsActive);
        result.CreatedAt.Should().Be(form.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var query = new GetMedicineDosageFormByIdQuery(Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MedicineDosageForm?)null);

        var handler = new GetMedicineDosageFormByIdHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'MedicineDosageForm' with key '{query.Id}' was not found.");
    }
}
