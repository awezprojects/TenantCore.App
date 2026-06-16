using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Handlers;

public class DeleteDosageRemarkHandlerTests
{
    private readonly Mock<IDosageRemarkRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_DeletesRemark()
    {
        var remark = DosageRemark.Create(Guid.NewGuid(), MedicineFormType.Tab, "Take after food", null, null);
        _repository.Setup(r => r.GetByIdAsync(remark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remark);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        accessValidator.Setup(v => v.CanAccess(remark.ApplicationId)).Returns(true);
        var handler = new DeleteDosageRemarkHandler(_repository.Object, accessValidator.Object);
        var command = new DeleteDosageRemarkCommand(remark.Id, remark.ApplicationId);

        await handler.Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(remark), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteDosageRemarkCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DosageRemark?)null);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        var handler = new DeleteDosageRemarkHandler(_repository.Object, accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'DosageRemark' with key '{command.Id}' was not found.");
        _repository.Verify(r => r.Delete(It.IsAny<DosageRemark>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var remark = DosageRemark.Create(Guid.NewGuid(), MedicineFormType.Capsule, "Take once daily", null, null);
        _repository.Setup(r => r.GetByIdAsync(remark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remark);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        accessValidator.Setup(v => v.CanAccess(remark.ApplicationId)).Returns(false);
        var handler = new DeleteDosageRemarkHandler(_repository.Object, accessValidator.Object);
        var command = new DeleteDosageRemarkCommand(remark.Id, Guid.NewGuid());

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'DosageRemark' with key '{command.Id}' was not found.");
        _repository.Verify(r => r.Delete(It.IsAny<DosageRemark>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
