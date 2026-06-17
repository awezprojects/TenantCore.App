using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class DosageRemarksControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly DosageRemarksController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public DosageRemarksControllerTests()
    {
        _controller = new DosageRemarksController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/dosage-remarks ?????????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var paged = new PagedResult<DosageRemarkDto>
        {
            Items = [BuildDto()], TotalCount = 1, Page = 1, PageSize = 50
        };
        _sender.Setup(s => s.Send(It.IsAny<GetDosageRemarksQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(paged);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(paged);
    }

    [Fact]
    public async Task GetAll_PassesApplicationIdFromContext()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDosageRemarksQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDosageRemarksQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_PassesFormFilter_WhenProvided()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDosageRemarksQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(form: MedicineFormType.Tab, ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDosageRemarksQuery>(q => q.Form == MedicineFormType.Tab),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/dosage-remarks/{id} ????????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetDosageRemarkByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetDosageRemarkByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDosageRemarkByIdQuery>(q => q.Id == id && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/dosage-remarks ????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateDosageRemarkCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(BuildCreateDto(), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(DosageRemarksController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateDosageRemarkCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(BuildCreateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateDosageRemarkCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/dosage-remarks/{id} ????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateDosageRemarkCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateDosageRemarkCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateDosageRemarkCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/dosage-remarks/{id} ????????????????????????????????????

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteDosageRemarkCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeleteDosageRemarkCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.Delete(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DeleteDosageRemarkCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static DosageRemarkDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        MedicineForm = MedicineFormType.Tab,
        RemarkEnglish = "Take after food",
        IsActive = true
    };

    private static CreateDosageRemarkDto BuildCreateDto() =>
        new(MedicineFormType.Tab, "Take after food", null, null);

    private static UpdateDosageRemarkDto BuildUpdateDto() =>
        new(MedicineFormType.Tab, "Take after food", null, null, true);

    private static PagedResult<DosageRemarkDto> EmptyPagedResult() =>
        new PagedResult<DosageRemarkDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 50 };
}
