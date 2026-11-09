using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

// DEF-FB-03: a user holding Clinic Admin (or Manager/SystemAdmin) on a clinic genuinely has
// admin rights there — that's correct. The gap was that choosing "Doctor Panel" in the UI had
// no effect server-side if the same session called these endpoints directly. The client now
// declares that choice via the X-View-Scope header (ClinicAuthorizationHandler); these tests
// confirm the controller actually honors it instead of only the role check.
public class FinanceReportsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly FinanceReportsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public FinanceReportsControllerTests()
    {
        _controller = new FinanceReportsController(_sender.Object);
        SetUser([]);
    }

    private void SetUser(IEnumerable<string> roles, bool doctorPanelView = false)
    {
        var appRolesPayload = roles.Select(roleName => new
        {
            appId = _applicationId,
            appName = "Test Clinic",
            roleId = Guid.NewGuid(),
            roleName
        });

        var identity = new ClaimsIdentity(
        [
            new Claim("nameid", _userId.ToString()),
            new Claim("app_roles", JsonSerializer.Serialize(appRolesPayload))
        ], "TestAuth");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        if (doctorPanelView)
            httpContext.Request.Headers["X-View-Scope"] = "Doctor";

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task GetDashboard_ClinicAdmin_NotInDoctorPanel_ReturnsOk()
    {
        SetUser(["Clinic Admin"], doctorPanelView: false);
        _sender.Setup(s => s.Send(It.IsAny<GetFinanceDashboardSummaryQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new FinanceDashboardSummaryDto());

        var result = await _controller.GetDashboard(ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDashboard_ClinicAdmin_InDoctorPanelView_ReturnsForbid()
    {
        // The exact DEF-FB-03 scenario: a Doctor + Clinic Admin user who chose "Doctor Panel".
        SetUser(["Doctor", "Clinic Admin"], doctorPanelView: true);

        var result = await _controller.GetDashboard(ct: CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _sender.Verify(s => s.Send(It.IsAny<GetFinanceDashboardSummaryQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDiscounts_ClinicAdmin_InDoctorPanelView_ReturnsForbid()
    {
        SetUser(["Doctor", "Clinic Admin"], doctorPanelView: true);

        var result = await _controller.GetDiscounts(DateTime.Today, DateTime.Today, ct: CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetDoctorWise_ClinicAdmin_NotInDoctorPanel_HonorsRequestedDoctorUserId()
    {
        var otherDoctorId = Guid.NewGuid();
        SetUser(["Clinic Admin"], doctorPanelView: false);
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorWiseOpdCollectionQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new DoctorWiseCollectionDto());

        await _controller.GetDoctorWise(DateTime.Today, DateTime.Today, doctorUserId: otherDoctorId, ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDoctorWiseOpdCollectionQuery>(q => q.DoctorUserId == otherDoctorId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDoctorWise_ClinicAdmin_InDoctorPanelView_IgnoresRequestedDoctorUserId_ScopesToSelf()
    {
        // The exact DEF-FB-03 scenario for doctor-wise: before the fix this still returned
        // every doctor's data (or whichever doctorUserId was requested) because the admin role
        // alone decided the scope. Choosing "Doctor Panel" must now force self-scoping too.
        var otherDoctorId = Guid.NewGuid();
        SetUser(["Doctor", "Clinic Admin"], doctorPanelView: true);
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorWiseOpdCollectionQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new DoctorWiseCollectionDto());

        await _controller.GetDoctorWise(DateTime.Today, DateTime.Today, doctorUserId: otherDoctorId, ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetDoctorWiseOpdCollectionQuery>(q => q.DoctorUserId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDoctorWise_PlainDoctor_InDoctorPanelView_StillWorks()
    {
        // A plain Doctor (no admin/manager/reception role) must still be able to see their own
        // collection while in "Doctor Panel" — this endpoint is how "My Finance" loads its data.
        SetUser(["Doctor"], doctorPanelView: true);
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorWiseOpdCollectionQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new DoctorWiseCollectionDto());

        var result = await _controller.GetDoctorWise(DateTime.Today, DateTime.Today, ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(s => s.Send(
            It.Is<GetDoctorWiseOpdCollectionQuery>(q => q.DoctorUserId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
