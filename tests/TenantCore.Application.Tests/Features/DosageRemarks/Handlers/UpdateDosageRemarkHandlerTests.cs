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

public class UpdateDosageRemarkHandlerTests
{
    private readonly Mock<IDosageRemarkRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_UpdatesRemarkAndReturnsDto()
    {
        var remark = DosageRemark.Create(Guid.NewGuid(), MedicineFormType.Tab, "Old remark", null, null);
        _repository.Setup(r => r.GetByIdAsync(remark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remark);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        accessValidator.Setup(v => v.CanAccess(remark.ApplicationId)).Returns(true);
        var handler = new UpdateDosageRemarkHandler(_repository.Object, accessValidator.Object);
        var command = new UpdateDosageRemarkCommand(
            remark.Id,
            remark.ApplicationId,
            MedicineFormType.Syrup,
            "Updated remark",
            "अद्यतन टिप्पणी",
            "अद्ययावत टिप्पणी",
            false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(remark.Id);
        result.MedicineForm.Should().Be(command.MedicineForm);
        result.RemarkEnglish.Should().Be(command.RemarkEnglish);
        result.RemarkHindi.Should().Be(command.RemarkHindi);
        result.RemarkMarathi.Should().Be(command.RemarkMarathi);
        result.IsActive.Should().BeFalse();
        remark.MedicineForm.Should().Be(command.MedicineForm);
        remark.RemarkEnglish.Should().Be(command.RemarkEnglish);
        remark.RemarkHindi.Should().Be(command.RemarkHindi);
        remark.RemarkMarathi.Should().Be(command.RemarkMarathi);
        remark.IsActive.Should().BeFalse();
        _repository.Verify(r => r.Update(remark), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateDosageRemarkCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MedicineFormType.Tab,
            "Updated remark",
            null,
            null,
            true);
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DosageRemark?)null);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        var handler = new UpdateDosageRemarkHandler(_repository.Object, accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'DosageRemark' with key '{command.Id}' was not found.");
        _repository.Verify(r => r.Update(It.IsAny<DosageRemark>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var remark = DosageRemark.Create(Guid.NewGuid(), MedicineFormType.Tab, "Old remark", null, null);
        _repository.Setup(r => r.GetByIdAsync(remark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remark);

        var accessValidator = new Mock<IApplicationAccessValidator>();
        accessValidator.Setup(v => v.CanAccess(remark.ApplicationId)).Returns(false);
        var handler = new UpdateDosageRemarkHandler(_repository.Object, accessValidator.Object);
        var command = new UpdateDosageRemarkCommand(
            remark.Id,
            Guid.NewGuid(),
            MedicineFormType.Syrup,
            "Updated remark",
            null,
            null,
            true);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity 'DosageRemark' with key '{command.Id}' was not found.");
        _repository.Verify(r => r.Update(It.IsAny<DosageRemark>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
