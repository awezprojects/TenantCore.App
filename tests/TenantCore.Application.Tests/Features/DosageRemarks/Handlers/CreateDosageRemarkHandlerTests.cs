using FluentAssertions;
using Moq;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Handlers;

public class CreateDosageRemarkHandlerTests
{
    private readonly Mock<IDosageRemarkRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenValid_CreatesRemarkAndReturnsDto()
    {
        var applicationId = Guid.NewGuid();
        var command = new CreateDosageRemarkCommand(
            applicationId,
            MedicineFormType.Tab,
            "Take after food",
            "भोजन के बाद लें",
            "जेवणानंतर घ्या");

        DosageRemark? captured = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<DosageRemark>(), It.IsAny<CancellationToken>()))
            .Callback<DosageRemark, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateDosageRemarkHandler(_repository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.ApplicationId.Should().Be(applicationId);
        result.MedicineForm.Should().Be(command.MedicineForm);
        result.RemarkEnglish.Should().Be(command.RemarkEnglish);
        result.RemarkHindi.Should().Be(command.RemarkHindi);
        result.RemarkMarathi.Should().Be(command.RemarkMarathi);
        result.IsActive.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.ApplicationId.Should().Be(applicationId);
        captured.MedicineForm.Should().Be(command.MedicineForm);
        captured.RemarkEnglish.Should().Be(command.RemarkEnglish);
        captured.RemarkHindi.Should().Be(command.RemarkHindi);
        captured.RemarkMarathi.Should().Be(command.RemarkMarathi);
        _repository.Verify(r => r.AddAsync(It.IsAny<DosageRemark>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSaveFails_PropagatesException()
    {
        var command = new CreateDosageRemarkCommand(
            Guid.NewGuid(),
            MedicineFormType.Capsule,
            "Take twice daily",
            null,
            null);

        _repository.Setup(r => r.AddAsync(It.IsAny<DosageRemark>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var handler = new CreateDosageRemarkHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");
        _repository.Verify(r => r.AddAsync(It.IsAny<DosageRemark>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
