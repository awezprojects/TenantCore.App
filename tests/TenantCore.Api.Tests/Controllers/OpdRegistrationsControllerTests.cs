using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Queries;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class OpdRegistrationsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly OpdRegistrationsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public OpdRegistrationsControllerTests()
    {
        _controller = new OpdRegistrationsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/opd-registrations ??????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var paged = new PagedResult<OpdRegistrationDto>
        {
            Items = [BuildDto()], TotalCount = 1, Page = 1, PageSize = 20
        };
        _sender.Setup(s => s.Send(It.IsAny<GetOpdRegistrationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(paged);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(paged);
    }

    [Fact]
    public async Task GetAll_PassesApplicationIdFromContext()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetOpdRegistrationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetOpdRegistrationsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_PassesFilterParameters_ToSender()
    {
        var doctorId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetOpdRegistrationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(page: 2, pageSize: 5, search: "John",
            doctorUserId: doctorId, todayOnly: true, ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetOpdRegistrationsQuery>(q =>
                q.Page == 2 && q.PageSize == 5 &&
                q.Search == "John" && q.DoctorUserId == doctorId &&
                q.TodayOnly == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/opd-registrations/{id} ????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetOpdRegistrationByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetOpdRegistrationByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetOpdRegistrationByIdQuery>(q => q.Id == id && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/opd-registrations ?????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateOpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(BuildCreateDto(), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(OpdRegistrationsController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateOpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(BuildCreateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateOpdRegistrationCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/opd-registrations/{id} ????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateOpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateOpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateOpdRegistrationCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/opd-registrations/doctor-count ????????????????????????????

    [Fact]
    public async Task GetDoctorCount_ReturnsOk_WithCount()
    {
        var doctorId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorOpdCountQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(7);

        var result = await _controller.GetDoctorCount(doctorId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(7);
    }

    [Fact]
    public async Task GetDoctorCount_SendsQueryWithCorrectDoctorIdAndApplicationId()
    {
        var doctorId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorOpdCountQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(0);

        await _controller.GetDoctorCount(doctorId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDoctorOpdCountQuery>(q => q.DoctorUserId == doctorId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static OpdRegistrationDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        PatientName = "Jane Doe",
        DoctorUserId = Guid.NewGuid(),
        DoctorName = "Dr. Smith",
        RegistrationNumber = "OPD-001",
        Status = OpdStatus.Pending
    };

    private static CreateOpdRegistrationDto BuildCreateDto() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", 200m, null);

    private static UpdateOpdRegistrationDto BuildUpdateDto() => new(
        Guid.NewGuid(), "Dr. Smith", 200m, OpdStatus.Pending, null);

    private static PagedResult<OpdRegistrationDto> EmptyPagedResult() =>
        new PagedResult<OpdRegistrationDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
}
