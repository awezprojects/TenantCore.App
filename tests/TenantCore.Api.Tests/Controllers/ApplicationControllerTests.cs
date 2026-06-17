using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Api.Tests.Controllers;

public class ApplicationControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ApplicationController _controller;

    public ApplicationControllerTests()
    {
        _controller = new ApplicationController(_sender.Object);
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

        var result = await _controller.InviteUserAsync(Guid.NewGuid(), BuildInviteRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<InvitationResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeSameAs(invitation);
    }

    // ?? POST /api/Application/invite-existing-user ?????????????????????????

    [Fact]
    public async Task InviteExistingUserAsync_ReturnsOk_WithSuccessMessage()
    {
        _sender.Setup(s => s.Send(It.IsAny<InviteExistingUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.InviteExistingUserAsync(Guid.NewGuid(), BuildInviteExistingRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
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

        var result = await _controller.ReinviteUserAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    // ?? POST /api/Application/{applicationId}/users/{userId}/assign ????????

    [Fact]
    public async Task AssignUserToApplicationAsync_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<AssignUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.AssignUserToApplicationAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    // ?? POST /api/Application/{applicationId}/users/{userId}/mapping ???????

    [Fact]
    public async Task AddApplicationUserMappingAsync_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<AddUserMappingCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.AddApplicationUserMappingAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    // ?? DELETE /api/Application/{applicationId}/users/{userId} ?????????????

    [Fact]
    public async Task RemoveUserFromApplicationAsync_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<RemoveUserCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.RemoveUserFromApplicationAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
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
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new ChangeUserRoleRequestDto { NewRoleId = Guid.NewGuid() }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
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
