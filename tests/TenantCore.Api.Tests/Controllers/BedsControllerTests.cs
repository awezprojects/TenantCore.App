using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Application.Features.Beds.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class BedsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly BedsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public BedsControllerTests()
    {
        _controller = new BedsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/beds ???????????????????????????????????????????????????????

    [Fact]
    public async Task GetByRoom_ReturnsOk_WithBedList()
    {
        var beds = new List<BedDto> { BuildDto() };
        var roomId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetBedsByRoomQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(beds);

        var result = await _controller.GetByRoom(roomId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(beds);
    }

    [Fact]
    public async Task GetByRoom_SendsQueryWithCorrectRoomIdAndApplicationId()
    {
        var roomId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetBedsByRoomQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<BedDto>());

        await _controller.GetByRoom(roomId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetBedsByRoomQuery>(q => q.RoomId == roomId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/beds/available ?????????????????????????????????????????????

    [Fact]
    public async Task GetAvailable_ReturnsOk_WithAvailableBeds()
    {
        var beds = new List<BedDto> { BuildDto() };
        _sender.Setup(s => s.Send(It.IsAny<GetAvailableBedsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(beds);

        var result = await _controller.GetAvailable(null, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(beds);
    }

    [Fact]
    public async Task GetAvailable_SendsQueryWithApplicationIdAndOptionalWardId()
    {
        var wardId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetAvailableBedsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<BedDto>());

        await _controller.GetAvailable(wardId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetAvailableBedsQuery>(q => q.ApplicationId == _applicationId && q.WardId == wardId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/beds ??????????????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateBedCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(new CreateBedDto(Guid.NewGuid(), "B1"), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BedsController.GetByRoom));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateBedCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(new CreateBedDto(Guid.NewGuid(), "B1"), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateBedCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/beds/{id} ???????????????????????????????????????????????

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteBedCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeleteBedCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.Delete(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DeleteBedCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static BedDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        RoomId = Guid.NewGuid(),
        BedNumber = "B1",
        IsActive = true
    };
}
