using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

// DEF-FB-03: same fix as FinanceReportsController — the clinic-wide cash-session list must
// also be denied to a caller who has explicitly chosen to view this clinic as "Doctor" this
// session, even if their role would otherwise allow it.
public class CounterSessionsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly CounterSessionsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public CounterSessionsControllerTests()
    {
        _controller = new CounterSessionsController(_sender.Object);
    }

    private void SetUser(IEnumerable<string> roles, bool doctorPanelView)
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
            new Claim("nameid", Guid.NewGuid().ToString()),
            new Claim("app_roles", JsonSerializer.Serialize(appRolesPayload))
        ], "TestAuth");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        if (doctorPanelView)
            httpContext.Request.Headers["X-View-Scope"] = "Doctor";

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task GetAll_ClinicAdmin_NotInDoctorPanel_ReturnsOk()
    {
        SetUser(["Clinic Admin"], doctorPanelView: false);
        _sender.Setup(s => s.Send(It.IsAny<GetCounterSessionsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<CounterSessionSummaryDto>());

        var result = await _controller.GetAll(ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ClinicAdmin_InDoctorPanelView_ReturnsForbid()
    {
        SetUser(["Doctor", "Clinic Admin"], doctorPanelView: true);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _sender.Verify(s => s.Send(It.IsAny<GetCounterSessionsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
