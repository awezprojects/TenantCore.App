using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class IpdRegistrationsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly IpdRegistrationsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public IpdRegistrationsControllerTests()
    {
        _controller = new IpdRegistrationsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/ipd-registrations ??????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var paged = new PagedResult<IpdRegistrationDto>
        {
            Items = [BuildDto()], TotalCount = 1, Page = 1, PageSize = 20
        };
        _sender.Setup(s => s.Send(It.IsAny<GetIpdRegistrationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(paged);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(paged);
    }

    [Fact]
    public async Task GetAll_PassesApplicationIdFromContext()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetIpdRegistrationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetIpdRegistrationsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/ipd-registrations/{id} ????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetIpdRegistrationByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetIpdRegistrationByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetIpdRegistrationByIdQuery>(q => q.Id == id && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/ipd-registrations ?????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateIpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(BuildCreateDto(), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(IpdRegistrationsController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateIpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(BuildCreateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateIpdRegistrationCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/ipd-registrations/{id} ????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateIpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateIpdRegistrationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateIpdRegistrationCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PATCH /api/ipd-registrations/{id}/discharge ????????????????????????

    [Fact]
    public async Task Discharge_ReturnsOk_WithDischargedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<DischargePatientCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Discharge(id, new DischargePatientDto("Discharged well"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Discharge_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DischargePatientCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Discharge(id, new DischargePatientDto(null), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DischargePatientCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static IpdRegistrationDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        PatientName = "Jane Doe",
        DoctorUserId = Guid.NewGuid(),
        DoctorName = "Dr. Smith",
        AdmissionNumber = "IPD-001",
        Status = IpdStatus.Admitted
    };

    private static CreateIpdRegistrationDto BuildCreateDto() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith",
        "Ward A", "101", "B1", 500m, null);

    private static UpdateIpdRegistrationDto BuildUpdateDto() => new(
        Guid.NewGuid(), "Dr. Smith", "Ward A", "101", "B1",
        IpdStatus.Admitted, null);

    private static PagedResult<IpdRegistrationDto> EmptyPagedResult() =>
        new PagedResult<IpdRegistrationDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
}
