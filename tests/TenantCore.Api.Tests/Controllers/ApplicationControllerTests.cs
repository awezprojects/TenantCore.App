using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Api.Tests.Controllers;

public class ApplicationControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ApplicationController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ApplicationControllerTests()
    {
        _controller = new ApplicationController(_sender.Object);

        // DEF-SEC-AUDIT-2026-11: InviteUserAsync/InviteExistingUserAsync/ReinviteUserAsync/
        // AssignUserToApplicationAsync/AddApplicationUserMappingAsync/RemoveUserFromApplicationAsync/
        // ChangeUserRoleAsync no longer accept the actor id (invitedBy/assignedBy/removedBy/
        // modifiedBy/reinvitedBy) as a request parameter — they derive it from the authenticated
        // caller via ClinicControllerBase.GetCurrentUserId(), which requires a "nameid" claim.
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

    // ?? POST /api/Application ???????????????????????????????????????????????

    [Fact]
    public async Task CreateApplicationAsync_ReturnsOk_WithSuccessResponse_WhenCreated()
    {
        var appResponse = BuildAppResponse();
        _sender.Setup(s => s.Send(It.IsAny<CreateApplicationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(appResponse);

        var result = await _controller.CreateApplicationAsync(Guid.NewGuid(), BuildCreationRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeSameAs(appResponse);
    }

    [Fact]
    public async Task CreateApplicationAsync_ReturnsOk_WithFailureResponse_WhenResultIsNull()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateApplicationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((ApplicationResponseDto?)null);

        var result = await _controller.CreateApplicationAsync(Guid.NewGuid(), BuildCreationRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    // ?? PUT /api/Application/{applicationId} ???????????????????????????????

    [Fact]
    public async Task EditApplicationAsync_ReturnsOk_WithSuccessResponse()
    {
        var appId = Guid.NewGuid();
        var appResponse = BuildAppResponse();
        _sender.Setup(s => s.Send(It.IsAny<EditApplicationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(appResponse);

        var result = await _controller.EditApplicationAsync(appId, BuildCreationRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task EditApplicationAsync_SendsCommandWithCorrectApplicationId()
    {
        var appId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<EditApplicationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildAppResponse());

        await _controller.EditApplicationAsync(appId, BuildCreationRequest(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<EditApplicationCommand>(c => c.ApplicationId == appId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/Application/{applicationId} ???????????????????????????????

    [Fact]
    public async Task GetApplicationByIdAsync_ReturnsOk_WhenFound()
    {
        var appId = Guid.NewGuid();
        var appResponse = BuildAppResponse();
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(appResponse);

        var result = await _controller.GetApplicationByIdAsync(appId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Data.Should().BeSameAs(appResponse);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ReturnsNotFound_WhenMissing()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((ApplicationResponseDto?)null);

        var result = await _controller.GetApplicationByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ?? GET /api/Application/by-code/{code} ????????????????????????????????

    [Fact]
    public async Task GetApplicationByCodeAsync_ReturnsOk_WhenFound()
    {
        var appResponse = BuildAppResponse();
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationByCodeQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(appResponse);

        var result = await _controller.GetApplicationByCodeAsync("TC001", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ApplicationResponseDto>>().Subject;
        response.Data.Should().BeSameAs(appResponse);
    }

    [Fact]
    public async Task GetApplicationByCodeAsync_ReturnsNotFound_WhenMissing()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationByCodeQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((ApplicationResponseDto?)null);

        var result = await _controller.GetApplicationByCodeAsync("NONE", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ?? GET /api/Application/get-all ???????????????????????????????????????

    [Fact]
    public async Task GetAllApplicationsAsync_ReturnsOk_WithList()
    {
        var apps = new List<ApplicationResponseDto> { BuildAppResponse() };
        _sender.Setup(s => s.Send(It.IsAny<GetAllApplicationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(apps);

        var result = await _controller.GetAllApplicationsAsync(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<ApplicationResponseDto>>>().Subject;
        response.Data.Should().BeSameAs(apps);
    }

    // ?? GET /api/Application/by-type/{applicationType} ?????????????????????

    [Fact]
    public async Task GetApplicationsByTypeAsync_ReturnsOk_WithFilteredList()
    {
        var apps = new List<ApplicationResponseDto> { BuildAppResponse() };
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationsByTypeQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(apps);

        var result = await _controller.GetApplicationsByTypeAsync(1, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<ApplicationResponseDto>>>().Subject;
        response.Data.Should().BeSameAs(apps);
    }

    // ?? GET /api/Application/{applicationId}/doctors ???????????????????????

    [Fact]
    public async Task GetDoctorsAsync_ReturnsOk_WithDoctorList()
    {
        var doctors = new List<DoctorDto> { new() { UserId = Guid.NewGuid(), FullName = "Dr. Smith" } };
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorsByApplicationQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(doctors);

        var result = await _controller.GetDoctorsAsync(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<DoctorDto>>>().Subject;
        response.Data.Should().BeSameAs(doctors);
    }

    // ?? GET /api/Application/{applicationId}/users ?????????????????????????

    [Fact]
    public async Task GetApplicationUsersAsync_ReturnsOk_WithUserList()
    {
        var users = new List<ApplicationUserResponseDto> { new() { UserId = Guid.NewGuid(), FullName = "Jane Doe" } };
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationUsersQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(users);

        var result = await _controller.GetApplicationUsersAsync(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<ApplicationUserResponseDto>>>().Subject;
        response.Data.Should().BeSameAs(users);
    }

    // ?? POST /api/Application/invite-user ??????????????????????????????????

    [Fact]
    public async Task InviteUserAsync_ReturnsOk_WithInvitationResponse()
    {
        var invitation = new InvitationResponseDto();
        _sender.Setup(s => s.Send(It.IsAny<InviteUserCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(invitation);

        var result = await _controller.InviteUserAsync(BuildInviteRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<InvitationResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeSameAs(invitation);
    }

    // Vulnerability scenario (item 3): invitedBy used to be a client-supplied query parameter —
    // a malicious/buggy client could attribute the invite to anyone. It must now always be the
    // authenticated caller's own id, regardless of anything the client sends.
    [Fact]
    public async Task InviteUserAsync_SendsCommandWithCallerIdAsInvitedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<InviteUserCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new InvitationResponseDto());

        await _controller.InviteUserAsync(BuildInviteRequest(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<InviteUserCommand>(c => c.InvitedBy == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/Application/invite-existing-user ?????????????????????????

    [Fact]
    public async Task InviteExistingUserAsync_ReturnsOk_WithSuccessMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<InviteExistingUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.InviteExistingUserAsync(BuildInviteExistingRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task InviteExistingUserAsync_SendsCommandWithCallerIdAsInvitedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<InviteExistingUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.InviteExistingUserAsync(BuildInviteExistingRequest(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<InviteExistingUserCommand>(c => c.InvitedBy == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/Application/{applicationId}/users/deactivated ?????????????

    [Fact]
    public async Task GetDeactivatedApplicationUsersAsync_ReturnsOk_WithDeactivatedUsers()
    {
        var users = new List<ApplicationUserResponseDto> { new() { UserId = Guid.NewGuid(), IsActive = false } };
        _sender.Setup(s => s.Send(It.IsAny<GetDeactivatedApplicationUsersQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(users);

        var result = await _controller.GetDeactivatedApplicationUsersAsync(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<ApplicationUserResponseDto>>>().Subject;
        response.Data.Should().BeSameAs(users);
    }

    // ?? DELETE /api/Application/{applicationId}/invitations/{invitationId} ?

    [Fact]
    public async Task DeleteInvitationAsync_ReturnsOk_WithSuccessMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteInvitationCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.DeleteInvitationAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    // ?? GET /api/Application/{applicationId}/invitations ???????????????????

    [Fact]
    public async Task GetApplicationInvitationsAsync_ReturnsOk_WithInvitationList()
    {
        var invitations = new List<InvitationResponseDto> { new() };
        _sender.Setup(s => s.Send(It.IsAny<GetApplicationInvitationsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(invitations);

        var result = await _controller.GetApplicationInvitationsAsync(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<InvitationResponseDto>>>().Subject;
        response.Data.Should().BeSameAs(invitations);
    }

    // ?? POST /api/Application/{applicationId}/invitations/{invitationId}/reinvite

    [Fact]
    public async Task ReinviteUserAsync_ReturnsOk_WithSuccessMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<ReinviteUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.ReinviteUserAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ReinviteUserAsync_SendsCommandWithCallerIdAsReinvitedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<ReinviteUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.ReinviteUserAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<ReinviteUserCommand>(c => c.ReinvitedBy == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── POST /api/Application/{applicationId}/users/{userId}/assign ────────
    // CRITICAL fix (item 1): this action previously had no [Authorize] override, inheriting only
    // the class-level RequireAuthenticated — any logged-in user of any clinic could grant any
    // role (including Clinic Admin) at any clinic, with assignedBy also forgeable via the query
    // string. See Action_RequiresClinicAdminPolicy below for the authorization-attribute
    // assertion and ClinicRoleAuthorizationHandlerTests for the cross-tenant-isolation proof
    // (the handler checks the "applicationId" route value generically for every action that
    // uses RequireClinicAdmin, including this one).

    [Fact]
    public async Task AssignUserToApplicationAsync_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<AssignUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.AssignUserToApplicationAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AssignUserToApplicationAsync_SendsCommandWithCallerIdAsAssignedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<AssignUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var applicationId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        await _controller.AssignUserToApplicationAsync(applicationId, targetUserId, roleId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<AssignUserCommand>(c =>
                c.ApplicationId == applicationId &&
                c.UserId == targetUserId &&
                c.RoleId == roleId &&
                c.AssignedBy == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/Application/{applicationId}/users/{userId}/mapping ???????
    // CRITICAL fix (item 1): same gap/fix as AssignUserToApplicationAsync above.

    [Fact]
    public async Task AddApplicationUserMappingAsync_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<AddUserMappingCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.AddApplicationUserMappingAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AddApplicationUserMappingAsync_SendsCommandWithCallerIdAsAssignedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<AddUserMappingCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.AddApplicationUserMappingAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<AddUserMappingCommand>(c => c.AssignedBy == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/Application/{applicationId}/users/{userId} ?????????????

    [Fact]
    public async Task RemoveUserFromApplicationAsync_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<RemoveUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.RemoveUserFromApplicationAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task RemoveUserFromApplicationAsync_SendsCommandWithCallerIdAsRemovedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<RemoveUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.RemoveUserFromApplicationAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<RemoveUserCommand>(c => c.RemovedBy == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/Application/{applicationId} ?????????????????????????????

    [Fact]
    public async Task DeleteApplicationAsync_ReturnsNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteApplicationCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.DeleteApplicationAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    // ?? PATCH /api/Application/{applicationId}/status ??????????????????????

    [Fact]
    public async Task ToggleApplicationStatusAsync_ReturnsOk_WithStatusMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<ToggleApplicationStatusCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.ToggleApplicationStatusAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new ToggleStatusRequestDto { IsActive = true }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    // ?? PATCH /api/Application/{applicationId}/users/{userId}/status ???????

    [Fact]
    public async Task ToggleUserApplicationMappingAsync_ReturnsOk_WithStatusMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<ToggleUserApplicationMappingCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.ToggleUserApplicationMappingAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new ToggleStatusRequestDto { IsActive = false }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    // ?? PUT /api/Application/{applicationId}/users/{userId}/role ???????????

    [Fact]
    public async Task ChangeUserRoleAsync_ReturnsOk_WithSuccessMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<ChangeUserRoleCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.ChangeUserRoleAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new ChangeUserRoleRequestDto { NewRoleId = Guid.NewGuid() }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeUserRoleAsync_SendsCommandWithCallerIdAsModifiedBy()
    {
        _sender.Setup(s => s.Send(It.IsAny<ChangeUserRoleCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var newRoleId = Guid.NewGuid();

        await _controller.ChangeUserRoleAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new ChangeUserRoleRequestDto { NewRoleId = newRoleId }, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<ChangeUserRoleCommand>(c => c.ModifiedBy == _userId && c.NewRoleId == newRoleId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Authorization (items 1 & 2): actions that used to only require RequireAuthenticated
    // (any logged-in user, any clinic) must now require RequireClinicAdmin — matching the
    // 9 siblings that were already correctly guarded on this controller. ──────────────────────

    [Theory]
    [InlineData(nameof(ApplicationController.EditApplicationAsync))]
    [InlineData(nameof(ApplicationController.GetApplicationUsersAsync))]
    [InlineData(nameof(ApplicationController.GetApplicationInvitationsAsync))]
    [InlineData(nameof(ApplicationController.GetDeactivatedApplicationUsersAsync))]
    [InlineData(nameof(ApplicationController.AssignUserToApplicationAsync))]
    [InlineData(nameof(ApplicationController.AddApplicationUserMappingAsync))]
    public void Action_RequiresClinicAdminPolicy(string actionName)
    {
        var method = typeof(ApplicationController).GetMethod(actionName);
        var attribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>().SingleOrDefault();

        attribute.Should().NotBeNull($"{actionName} must have its own [Authorize] override");
        attribute!.Policy.Should().Be(AuthPolicies.RequireClinicAdmin);
    }

    // GetAllApplicationsAsync has no single applicationId to scope against (genuine
    // cross-tenant "list everything") — restricted to System Admin instead (see item 2).
    [Fact]
    public void GetAllApplicationsAsync_RequiresSystemAdminPolicy()
    {
        var method = typeof(ApplicationController).GetMethod(nameof(ApplicationController.GetAllApplicationsAsync));
        var attribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>().SingleOrDefault();

        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be(AuthPolicies.RequireSystemAdmin);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static ApplicationResponseDto BuildAppResponse() => new()
    {
        ApplicationId = Guid.NewGuid(),
        ApplicationName = "Test Clinic",
        ApplicationCode = "TC001",
        IsActive = true
    };

    private static ApplicationCreationRequestDto BuildCreationRequest() => new()
    {
        ApplicationName = "Test Clinic",
        ApplicationCode = "TC001",
        ApplicationType = 1
    };

    private static InviteUserRequestDto BuildInviteRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        EmailId = "jane@example.com",
        ApplicationId = Guid.NewGuid(),
        RoleId = Guid.NewGuid()
    };

    private static InviteExistingUserRequestDto BuildInviteExistingRequest() => new()
    {
        UserId = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        RoleId = Guid.NewGuid()
    };
}
