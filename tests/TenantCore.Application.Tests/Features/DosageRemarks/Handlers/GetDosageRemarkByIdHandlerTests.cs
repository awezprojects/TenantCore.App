using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.DosageRemarks.Handlers;
using TenantCore.Application.Features.DosageRemarks.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Handlers;

public class GetDosageRemarkByIdHandlerTests
{
    private readonly Mock<IDosageRemarkRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_ReturnsMappedDto()
    {
        var remark = DosageRemark.Create(Guid.NewGuid(), MedicineFormType.Syrup, "Shake well", "अच्छी तरह हिलाएँ", null);
        _repository.Setup(r => r.GetByIdAsync(remark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remark);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        accessValidator.Setup(v => v.CanAccess(remark.ApplicationId)).Returns(true);
        var handler = new GetDosageRemarkByIdHandler(_repository.Object, accessValidator.Object);
        var query = new GetDosageRemarkByIdQuery(remark.Id, remark.ApplicationId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(remark.Id);
        result.ApplicationId.Should().Be(remark.ApplicationId);
        result.MedicineForm.Should().Be(remark.MedicineForm);
        result.RemarkEnglish.Should().Be(remark.RemarkEnglish);
        result.RemarkHindi.Should().Be(remark.RemarkHindi);
        result.RemarkMarathi.Should().Be(remark.RemarkMarathi);
        result.IsActive.Should().Be(remark.IsActive);
        result.CreatedAt.Should().Be(remark.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var query = new GetDosageRemarkByIdQuery(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DosageRemark?)null);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        var handler = new GetDosageRemarkByIdHandler(_repository.Object, accessValidator.Object);

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'DosageRemark' with key '{query.Id}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var remark = DosageRemark.Create(Guid.NewGuid(), MedicineFormType.Injection, "Use as directed", null, null);
        _repository.Setup(r => r.GetByIdAsync(remark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remark);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        accessValidator.Setup(v => v.CanAccess(remark.ApplicationId)).Returns(false);
        var handler = new GetDosageRemarkByIdHandler(_repository.Object, accessValidator.Object);
        var query = new GetDosageRemarkByIdQuery(remark.Id, Guid.NewGuid());

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'DosageRemark' with key '{query.Id}' was not found.");
    }
}
