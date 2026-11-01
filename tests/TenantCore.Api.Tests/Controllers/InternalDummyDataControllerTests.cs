using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Api.Authentication;
using TenantCore.Api.Controllers.Internal;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Queries;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Api.Tests.Controllers;

/// <summary>
/// DummyData:Enabled is the only thing standing between these routes and a production database, so
/// the 403 is asserted on all three actions, and the route/verb/scheme triple — the cross-repo
/// contract TenantCore.Admin calls by hand — is locked the same way InternalClinicsController is.
/// </summary>
public class InternalDummyDataControllerTests
{
    private static readonly Guid ApplicationId = Guid.Parse("c1111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminUserId = Guid.Parse("0a0a0a0a-0000-0000-0000-000000000001");
    private const string AdminEmail = "admin@example.test";

    private readonly Mock<ISender> _sender = new();

    private InternalDummyDataController Controller(bool enabled)
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDummyDataStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DummyDataStatusDto { HasDummyData = true, PatientCount = 75 });
        _sender.Setup(s => s.Send(It.IsAny<SeedDummyDataCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DummyDataStatusDto { HasDummyData = true, PatientCount = 75 });
        _sender.Setup(s => s.Send(It.IsAny<PurgeDummyDataCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DummyDataStatusDto());

        return new InternalDummyDataController(
            _sender.Object, Options.Create(new DummyDataOptions { Enabled = enabled }));
    }

    private static AdminActionRequest Body() => new() { AdminUserId = AdminUserId, AdminEmail = AdminEmail };

    // ---------------------------------------------------------------- the flag gate

    [Fact]
    public async Task GetStatus_FlagOff_IsRefusedWith403AndNeverReachesAHandler()
    {
        var act = async () => await Controller(false).GetStatus(ApplicationId, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<ErrorCodeException>()).Which;
        error.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        error.ErrorCode.Should().Be("dummy_data_disabled");

        _sender.Verify(s => s.Send(It.IsAny<GetDummyDataStatusQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Seed_FlagOff_IsRefusedWith403AndNeverReachesAHandler()
    {
        var act = async () => await Controller(false).Seed(ApplicationId, Body(), CancellationToken.None);

        var error = (await act.Should().ThrowAsync<ErrorCodeException>()).Which;
        error.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        _sender.Verify(s => s.Send(It.IsAny<SeedDummyDataCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Purge_FlagOff_IsRefusedWith403AndNeverReachesAHandler()
    {
        var act = async () => await Controller(false).Purge(ApplicationId, Body(), CancellationToken.None);

        var error = (await act.Should().ThrowAsync<ErrorCodeException>()).Which;
        error.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        _sender.Verify(s => s.Send(It.IsAny<PurgeDummyDataCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- the happy paths

    [Fact]
    public async Task GetStatus_FlagOn_ReturnsTheStatus()
    {
        var result = await Controller(true).GetStatus(ApplicationId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ((DummyDataStatusDto)ok.Value!).PatientCount.Should().Be(75);
    }

    [Fact]
    public async Task Seed_FlagOn_Returns201WithTheStatus()
    {
        var result = await Controller(true).Seed(ApplicationId, Body(), CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((DummyDataStatusDto)created.Value!).PatientCount.Should().Be(75);
    }

    [Fact]
    public async Task Seed_TakesTheClinicFromTheRouteAndTheAdminFromTheBody()
    {
        await Controller(true).Seed(ApplicationId, Body(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<SeedDummyDataCommand>(c =>
                c.ApplicationId == ApplicationId
                && c.AdminUserId == AdminUserId
                && c.AdminEmail == AdminEmail),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_FlagOn_Returns200WithTheStatus()
    {
        var result = await Controller(true).Purge(ApplicationId, Body(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ((DummyDataStatusDto)ok.Value!).HasDummyData.Should().BeFalse();
    }

    // ---------------------------------------------------------------- the cross-repo contract

    private static IReadOnlyList<(string Verb, string? Template)> DeclaredActions()
    {
        var actions = new List<(string, string?)>();

        foreach (var method in typeof(InternalDummyDataController)
                     .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var attribute in method.GetCustomAttributes<HttpMethodAttribute>())
            {
                foreach (var verb in attribute.HttpMethods)
                    actions.Add((verb, string.IsNullOrEmpty(attribute.Template) ? null : attribute.Template));
            }
        }

        return actions;
    }

    [Fact]
    public void Route_MatchesThePortalContract()
    {
        typeof(InternalDummyDataController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/internal/clinics/{applicationId:guid}/dummy-data",
                "TenantCore.Admin calls these URLs verbatim; renaming one side alone is a 404");

        DeclaredActions().Should().BeEquivalentTo(
            new (string, string?)[]
            {
                ("GET", null),
                ("POST", "seed"),
                ("DELETE", null),
            });
    }

    [Fact]
    public void Controller_IsGuardedByTheInternalServiceSchemeOnly()
    {
        var authorize = typeof(InternalDummyDataController).GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull();
        authorize!.AuthenticationSchemes.Should().Be(InternalServiceKeyAuthenticationHandler.SchemeName);
        authorize.Policy.Should().BeNull("the service key is the gate — there is no user-facing policy here");
    }
}
