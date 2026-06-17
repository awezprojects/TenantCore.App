using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.Clinics.Commands;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Api.Tests.Controllers;

public class ClinicControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ClinicController _controller;

    public ClinicControllerTests()
    {
        _controller = new ClinicController(_sender.Object);
    }

    // ?? POST /api/Clinic ????????????????????????????????????????????????????

    [Fact]
    public async Task CreateClinic_ReturnsOk_WithSuccessResponse_WhenCreated()
    {
        var appResponse = BuildApplicationResponseDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateClinicCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(appResponse);

        var result = await _controller.CreateClinic(BuildCreateClinicRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeSameAs(appResponse);
    }

    [Fact]
    public async Task CreateClinic_ReturnsOk_WithFailureResponse_WhenResultIsNull()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateClinicCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((ApplicationResponseDto?)null);

        var result = await _controller.CreateClinic(BuildCreateClinicRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateClinic_SendsCommandWithRequest()
    {
        var request = BuildCreateClinicRequest();
        _sender.Setup(s => s.Send(It.IsAny<CreateClinicCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildApplicationResponseDto());

        await _controller.CreateClinic(request, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateClinicCommand>(c => c.Request == request),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/Clinic/dashboard ???????????????????????????????????????????

    [Fact]
    public async Task GetClinicDashboard_ReturnsOk_WithDashboardItems()
    {
        var items = new List<ClinicDashboardItemDto> { BuildDashboardItem() };
        _sender.Setup(s => s.Send(It.IsAny<GetClinicDashboardQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(items);

        var result = await _controller.GetClinicDashboard(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<ClinicDashboardItemDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeSameAs(items);
    }

    [Fact]
    public async Task GetClinicDashboard_ReturnsOk_WithEmptyList_WhenNoClinics()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetClinicDashboardQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<ClinicDashboardItemDto>());

        var result = await _controller.GetClinicDashboard(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<ClinicDashboardItemDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEmpty();
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static ApplicationResponseDto BuildApplicationResponseDto() => new()
    {
        ApplicationId = Guid.NewGuid(),
        ApplicationName = "Test Clinic",
        ApplicationCode = "TC001",
        IsActive = true
    };

    private static CreateClinicRequestDto BuildCreateClinicRequest() => new()
    {
        ClinicName = "Test Clinic",
        ClinicCode = "TC001"
    };

    private static ClinicDashboardItemDto BuildDashboardItem() => new()
    {
        ApplicationId = Guid.NewGuid(),
        ClinicName = "Test Clinic",
        ClinicCode = "TC001"
    };
}
