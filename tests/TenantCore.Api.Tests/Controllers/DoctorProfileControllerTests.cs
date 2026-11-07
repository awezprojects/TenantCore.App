using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.DoctorProfiles.Commands;
using TenantCore.Application.Features.DoctorProfiles.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class DoctorProfileControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly DoctorProfileController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public DoctorProfileControllerTests()
    {
        _controller = new DoctorProfileController(_sender.Object);

        var identity = new ClaimsIdentity(
        [
            new Claim("nameid", _userId.ToString())
        ], "TestAuth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/doctor-profile ?????????????????????????????????????????????

    [Fact]
    public async Task GetMyProfile_ReturnsOk_WhenProfileExists()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorProfileQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetMyProfile(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorProfileQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((DoctorProfileDto?)null);

        var result = await _controller.GetMyProfile(CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMyProfile_SendsQueryWithCurrentUserId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorProfileQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetMyProfile(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDoctorProfileQuery>(q => q.UserId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/doctor-profile ?????????????????????????????????????????????

    [Fact]
    public async Task UpsertMyProfile_ReturnsOk_WithUpdatedDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpsertDoctorProfileCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.UpsertMyProfile(BuildUpsertDto(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task UpsertMyProfile_SendsCommandWithCurrentUserId()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpsertDoctorProfileCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.UpsertMyProfile(BuildUpsertDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpsertDoctorProfileCommand>(c => c.UserId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // DEF-MR-11: these two actions are the doctor's own global profile, not clinic-scoped data —
    // they must authorize across any of the user's clinics, not the currently selected one
    // (RequireClinical would 403 whenever no clinic is selected at all).
    [Theory]
    [InlineData(nameof(DoctorProfileController.UpsertMyProfile))]
    [InlineData(nameof(DoctorProfileController.SetMyPrescriptionTemplate))]
    public void Action_RequiresAnyClinicalPolicy_NotClinicScoped(string actionName)
    {
        var method = typeof(DoctorProfileController).GetMethod(actionName);
        var attribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>().SingleOrDefault();

        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be(AuthPolicies.RequireAnyClinical);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static DoctorProfileDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        RegistrationNumber = "REG-001"
    };

    private static UpsertDoctorProfileDto BuildUpsertDto() => new()
    {
        RegistrationNumber = "REG-001",
        SpecialityId = Guid.NewGuid(),
        QualificationDetails = "MBBS"
    };
}
