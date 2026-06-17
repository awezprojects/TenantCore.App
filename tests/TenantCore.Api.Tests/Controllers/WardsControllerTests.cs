using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class WardsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly WardsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public WardsControllerTests()
    {
        _controller = new WardsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/wards ??????????????????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithWardList()
    {
        var wards = new List<WardDto> { BuildDto() };
        _sender.Setup(s => s.Send(It.IsAny<GetWardsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(wards);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(wards);
    }

    [Fact]
    public async Task GetAll_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetWardsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<WardDto>());

        await _controller.GetAll(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetWardsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/wards/{id} ?????????????????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetWardByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetWardByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetWardByIdQuery>(q => q.Id == id && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/wards ?????????????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateWardCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(new CreateWardDto("Ward A", null), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(WardsController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateWardCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(new CreateWardDto("Ward A", null), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateWardCommand>(c => c.ApplicationId == _applicationId && c.Name == "Ward A"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/wards/{id} ?????????????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateWardCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, new UpdateWardDto("Ward B", null), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateWardCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, new UpdateWardDto("Ward B", null), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateWardCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/wards/{id} ??????????????????????????????????????????????

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteWardCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeleteWardCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.Delete(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DeleteWardCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static WardDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        Name = "Ward A",
        IsActive = true
    };
}
