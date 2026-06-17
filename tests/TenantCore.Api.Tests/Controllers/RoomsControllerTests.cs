using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class RoomsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly RoomsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public RoomsControllerTests()
    {
        _controller = new RoomsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/rooms ??????????????????????????????????????????????????????

    [Fact]
    public async Task GetByWard_ReturnsOk_WithRoomList()
    {
        var rooms = new List<RoomDto> { BuildDto() };
        var wardId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetRoomsByWardQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(rooms);

        var result = await _controller.GetByWard(wardId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rooms);
    }

    [Fact]
    public async Task GetByWard_SendsQueryWithCorrectWardIdAndApplicationId()
    {
        var wardId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetRoomsByWardQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<RoomDto>());

        await _controller.GetByWard(wardId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetRoomsByWardQuery>(q => q.WardId == wardId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/rooms ?????????????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateRoomCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(BuildCreateDto(), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(RoomsController.GetByWard));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateRoomCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(BuildCreateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateRoomCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/rooms/{id} ?????????????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateRoomCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, new UpdateRoomDto("102", "General", 300m), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateRoomCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, new UpdateRoomDto("102", "General", 300m), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateRoomCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/rooms/{id} ??????????????????????????????????????????????

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteRoomCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeleteRoomCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.Delete(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DeleteRoomCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static RoomDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        WardId = Guid.NewGuid(),
        RoomNumber = "101",
        IsActive = true
    };

    private static CreateRoomDto BuildCreateDto() => new(Guid.NewGuid(), "101", "General", 250m);
}
