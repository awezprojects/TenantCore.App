using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class ClinicSettingsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ClinicSettingsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public ClinicSettingsControllerTests()
    {
        _controller = new ClinicSettingsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/clinic-settings/fees ??????????????????????????????????????

    [Fact]
    public async Task GetFees_ReturnsOk_WithFeeConfigDto()
    {
        var dto = new ClinicFeeConfigDto { Id = Guid.NewGuid(), ApplicationId = _applicationId, OpdFee = 150m };
        _sender.Setup(s => s.Send(It.IsAny<GetClinicFeeConfigQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetFees(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetFees_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetClinicFeeConfigQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ClinicFeeConfigDto());

        await _controller.GetFees(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetClinicFeeConfigQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/clinic-settings/fees ??????????????????????????????????????

    [Fact]
    public async Task UpdateFees_ReturnsOk_WithUpdatedDto()
    {
        var dto = new ClinicFeeConfigDto { OpdFee = 200m };
        _sender.Setup(s => s.Send(It.IsAny<UpdateClinicFeeConfigCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.UpdateFees(new UpdateClinicFeeConfigDto(200m), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task UpdateFees_SendsCommandWithCorrectFeeAndApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpdateClinicFeeConfigCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ClinicFeeConfigDto());

        await _controller.UpdateFees(new UpdateClinicFeeConfigDto(300m), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateClinicFeeConfigCommand>(c => c.ApplicationId == _applicationId && c.OpdFee == 300m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/clinic-settings/doctors ???????????????????????????????????

    [Fact]
    public async Task GetDoctors_ReturnsOk_WithDoctorList()
    {
        var doctors = new List<DoctorDto>
        {
            new() { UserId = Guid.NewGuid(), FullName = "Dr. Smith" }
        };
        _sender.Setup(s => s.Send(It.IsAny<GetClinicDoctorsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(doctors);

        var result = await _controller.GetDoctors(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(doctors);
    }

    [Fact]
    public async Task GetDoctors_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetClinicDoctorsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<DoctorDto>());

        await _controller.GetDoctors(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetClinicDoctorsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GET /api/clinic-settings/feature-flags ──────────────────────────────

    [Fact]
    public async Task GetFeatureFlags_ReturnsOk_WithFlagsDto()
    {
        var dto = new ClinicFeatureFlagsDto { Id = Guid.NewGuid(), ApplicationId = _applicationId, PrepaidOpdEnabled = true };
        _sender.Setup(s => s.Send(It.IsAny<GetClinicFeatureFlagsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetFeatureFlags(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetFeatureFlags_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetClinicFeatureFlagsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ClinicFeatureFlagsDto());

        await _controller.GetFeatureFlags(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetClinicFeatureFlagsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── PUT /api/clinic-settings/feature-flags ──────────────────────────────

    [Fact]
    public async Task UpdateFeatureFlags_ReturnsOk_WithUpdatedDto()
    {
        var dto = new ClinicFeatureFlagsDto { PrepaidOpdEnabled = false };
        _sender.Setup(s => s.Send(It.IsAny<UpdateClinicFeatureFlagsCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.UpdateFeatureFlags(new UpdateClinicFeatureFlagsDto(false), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task UpdateFeatureFlags_SendsCommandWithCorrectValueAndApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpdateClinicFeatureFlagsCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ClinicFeatureFlagsDto());

        await _controller.UpdateFeatureFlags(new UpdateClinicFeatureFlagsDto(false), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateClinicFeatureFlagsCommand>(c => c.ApplicationId == _applicationId && c.PrepaidOpdEnabled == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
