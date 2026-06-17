using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.PrescriptionConfig.Commands;
using TenantCore.Application.Features.PrescriptionConfig.Queries;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class PrescriptionConfigControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly PrescriptionConfigController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public PrescriptionConfigControllerTests()
    {
        _controller = new PrescriptionConfigController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/prescription-config ???????????????????????????????????????

    [Fact]
    public async Task Get_ReturnsOk_WithConfigDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionConfigQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Get(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Get_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionConfigQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Get(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPrescriptionConfigQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/prescription-config ???????????????????????????????????????

    [Fact]
    public async Task Upsert_ReturnsOk_WithUpdatedConfig()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpsertPrescriptionConfigCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Upsert(BuildUpdateDto(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Upsert_SendsCommandWithApplicationIdAndMappedFields()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpsertPrescriptionConfigCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Upsert(BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpsertPrescriptionConfigCommand>(c =>
                c.ApplicationId == _applicationId &&
                c.DefaultLanguage == PrescriptionLanguage.English),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static PrescriptionConfigDto BuildDto() => new()
    {
        ApplicationId = Guid.NewGuid(),
        DefaultLanguage = PrescriptionLanguage.English,
        PrintMarginTop = 10,
        PrintMarginRight = 10,
        PrintMarginBottom = 10,
        PrintMarginLeft = 10
    };

    private static UpdatePrescriptionConfigDto BuildUpdateDto() =>
        new(PrescriptionLanguage.English, 10, 10, 10, 10, false);
}
