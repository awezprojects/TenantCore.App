using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class MedicineDosageFormsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly MedicineDosageFormsController _controller;

    public MedicineDosageFormsControllerTests()
    {
        _controller = new MedicineDosageFormsController(_sender.Object);
    }

    // ?? GET /api/medicine-dosage-forms ??????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var pagedResult = new PagedResult<MedicineDosageFormDto>
        {
            Items = [BuildDto()], TotalCount = 1, Page = 1, PageSize = 50
        };
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineDosageFormsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(pagedResult);
    }

    [Fact]
    public async Task GetAll_PassesQueryParameters_ToSender()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineDosageFormsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(page: 2, pageSize: 10, search: "tab", ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicineDosageFormsQuery>(q => q.Page == 2 && q.PageSize == 10 && q.Search == "tab"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/medicine-dosage-forms/{id} ????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineDosageFormByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineDosageFormByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicineDosageFormByIdQuery>(q => q.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/medicine-dosage-forms ????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateMedicineDosageFormCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(new CreateMedicineDosageFormDto("Tablet", null), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(MedicineDosageFormsController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithNameAndDescription()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateMedicineDosageFormCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(new CreateMedicineDosageFormDto("Tablet", "oral"), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateMedicineDosageFormCommand>(c => c.Name == "Tablet" && c.Description == "oral"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/medicine-dosage-forms/{id} ????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateMedicineDosageFormCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, new UpdateMedicineDosageFormDto("Tablet", null, true), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateMedicineDosageFormCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, new UpdateMedicineDosageFormDto("Tablet", null, true), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateMedicineDosageFormCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/medicine-dosage-forms/{id} ?????????????????????????????

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteMedicineDosageFormCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeleteMedicineDosageFormCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.Delete(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DeleteMedicineDosageFormCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static MedicineDosageFormDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Tablet",
        IsActive = true
    };

    private static PagedResult<MedicineDosageFormDto> EmptyPagedResult() =>
        new PagedResult<MedicineDosageFormDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 50 };
}
