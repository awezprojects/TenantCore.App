using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class ObstetricControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ObstetricController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public ObstetricControllerTests()
    {
        _controller = new ObstetricController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/obstetric/prescriptions/{id}/dates ?????????????????????????

    [Fact]
    public async Task GetObstetricDates_ReturnsOk_WhenFound()
    {
        var dto = BuildDatesDto();
        _sender.Setup(s => s.Send(It.IsAny<GetObstetricDatesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetObstetricDates(dto.PrescriptionId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetObstetricDates_SendsQueryWithCorrectPrescriptionIdAndApplicationId()
    {
        var prescriptionId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetObstetricDatesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDatesDto());

        await _controller.GetObstetricDates(prescriptionId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetObstetricDatesQuery>(q => q.PrescriptionId == prescriptionId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/obstetric/patients/{id}/usg-chart ??????????????????????????

    [Fact]
    public async Task GetUsgChart_ReturnsOk_WithChartDto()
    {
        var patientId = Guid.NewGuid();
        var dto = new UsgChartDto { PatientId = patientId };
        _sender.Setup(s => s.Send(It.IsAny<GetUsgChartByPatientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetUsgChart(patientId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetUsgChart_SendsQueryWithCorrectPatientIdAndApplicationId()
    {
        var patientId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetUsgChartByPatientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new UsgChartDto());

        await _controller.GetUsgChart(patientId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetUsgChartByPatientQuery>(q => q.PatientId == patientId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/obstetric/prescriptions/{id}/lmp ???????????????????????????

    [Fact]
    public async Task SetLmp_ReturnsOk_WithDatesDto()
    {
        var prescriptionId = Guid.NewGuid();
        var dto = BuildDatesDto();
        _sender.Setup(s => s.Send(It.IsAny<SetObstetricLmpCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.SetLmp(prescriptionId,
            new SetLmpRequest { Lmp = new DateOnly(2024, 1, 1) }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task SetLmp_SendsCommandWithCorrectPrescriptionIdAndApplicationId()
    {
        var prescriptionId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<SetObstetricLmpCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDatesDto());

        await _controller.SetLmp(prescriptionId,
            new SetLmpRequest { Lmp = new DateOnly(2024, 1, 1) }, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<SetObstetricLmpCommand>(c => c.PrescriptionId == prescriptionId && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/obstetric/prescriptions/{id}/edd-by-usg ???????????????????

    [Fact]
    public async Task SetEddByUsg_ReturnsOk_WithDatesDto()
    {
        var prescriptionId = Guid.NewGuid();
        var dto = BuildDatesDto();
        _sender.Setup(s => s.Send(It.IsAny<SetObstetricEddByUsgCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.SetEddByUsg(prescriptionId,
            new SetEddByUsgRequest { EddByUsg = new DateOnly(2024, 10, 1) }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task SetEddByUsg_SendsCommandWithCorrectPrescriptionIdAndApplicationId()
    {
        var prescriptionId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<SetObstetricEddByUsgCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDatesDto());

        await _controller.SetEddByUsg(prescriptionId,
            new SetEddByUsgRequest { EddByUsg = new DateOnly(2024, 10, 1) }, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<SetObstetricEddByUsgCommand>(c => c.PrescriptionId == prescriptionId && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static ObstetricDatesDto BuildDatesDto() => new()
    {
        Id = Guid.NewGuid(),
        PrescriptionId = Guid.NewGuid(),
        Lmp = new DateOnly(2024, 1, 1)
    };
}
