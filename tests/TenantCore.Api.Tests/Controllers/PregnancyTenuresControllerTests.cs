using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.PregnancyTenures.Commands;
using TenantCore.Application.Features.PregnancyTenures.Queries;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class PregnancyTenuresControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly PregnancyTenuresController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public PregnancyTenuresControllerTests()
    {
        _controller = new PregnancyTenuresController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/PregnancyTenures/overdue ???????????????????????????????????

    [Fact]
    public async Task GetOverdue_ReturnsOk_WithSummaryList()
    {
        var summaries = new List<PregnancyTenureSummaryDto> { BuildSummaryDto() };
        _sender.Setup(s => s.Send(It.IsAny<GetOverdueEddPatientsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(summaries);

        var result = await _controller.GetOverdue(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(summaries);
    }

    [Fact]
    public async Task GetOverdue_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetOverdueEddPatientsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<PregnancyTenureSummaryDto>());

        await _controller.GetOverdue(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetOverdueEddPatientsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/PregnancyTenures/active/{patientId} ????????????????????????

    [Fact]
    public async Task GetActiveForPatient_ReturnsOk_WhenTenureExists()
    {
        var dto = BuildDto();
        var patientId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetActivePregnancyTenureForPatientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetActiveForPatient(patientId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetActiveForPatient_ReturnsNoContent_WhenNoActiveTenure()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetActivePregnancyTenureForPatientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((PregnancyTenureDto?)null);

        var result = await _controller.GetActiveForPatient(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetActiveForPatient_SendsQueryWithCorrectPatientIdAndApplicationId()
    {
        var patientId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetActivePregnancyTenureForPatientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetActiveForPatient(patientId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetActivePregnancyTenureForPatientQuery>(q => q.PatientId == patientId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/PregnancyTenures/patient/{patientId} ???????????????????????

    [Fact]
    public async Task GetForPatient_ReturnsOk_WithTenureList()
    {
        var tenures = new List<PregnancyTenureDto> { BuildDto() };
        var patientId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetPatientPregnancyTenuresQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(tenures);

        var result = await _controller.GetForPatient(patientId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(tenures);
    }

    [Fact]
    public async Task GetForPatient_SendsQueryWithCorrectPatientIdAndApplicationId()
    {
        var patientId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetPatientPregnancyTenuresQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<PregnancyTenureDto>());

        await _controller.GetForPatient(patientId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPatientPregnancyTenuresQuery>(q => q.PatientId == patientId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/PregnancyTenures/{id}/close ????????????????????????????????

    [Fact]
    public async Task Close_ReturnsOk_WithClosedTenureDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<ClosePregnancyTenureCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Close(id,
            new ClosePregnancyTenureRequest { Outcome = PregnancyOutcome.Delivered },
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Close_SendsCommandWithCorrectTenureIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<ClosePregnancyTenureCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Close(id,
            new ClosePregnancyTenureRequest { Outcome = PregnancyOutcome.Delivered },
            CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<ClosePregnancyTenureCommand>(c => c.TenureId == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static PregnancyTenureDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        Lmp = new DateOnly(2024, 1, 1),
        EddByLmp = new DateOnly(2024, 10, 8),
        EffectiveEdd = new DateOnly(2024, 10, 8),
        Status = PregnancyTenureStatus.Active
    };

    private static PregnancyTenureSummaryDto BuildSummaryDto() => new()
    {
        TenureId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        PatientFullName = "Jane Doe",
        Lmp = new DateOnly(2024, 1, 1),
        EddByLmp = new DateOnly(2024, 10, 8),
        EffectiveEdd = new DateOnly(2024, 10, 8),
        Status = PregnancyTenureStatus.Active
    };
}
