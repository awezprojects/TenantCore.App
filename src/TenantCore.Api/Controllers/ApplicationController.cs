using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Wrapper controller for Application management operations.
/// Dispatches commands and queries via MediatR; handlers delegate
/// to <c>IAuthApplicationService</c> (implemented in Infrastructure),
/// which forwards the request — including the caller's bearer token — to
/// the downstream TenantCore.Auth service.
/// </summary>
[ApiController]
[Route("api/Application")]
[Authorize]
[Produces("application/json")]
public class ApplicationController(ISender sender) : ControllerBase
{
    // POST /api/Application?ownerId={guid}
    [HttpPost]
    [ProducesResponseType(typeof(ApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateApplicationAsync(
        [FromQuery] Guid ownerId,
        [FromBody] ApplicationCreationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateApplicationCommand(ownerId, request), cancellationToken);
        var response = new ApiResponse<ApplicationResponseDto>
        {
            Success = result != null,
            Data = result,
            Message = result != null ? "Success" : "Failed to create application."
        };
        return Ok(response);
    }

    // PUT /api/Application/{applicationId}
    [HttpPut("{applicationId:guid}")]
    [ProducesResponseType(typeof(ApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditApplicationAsync(
        Guid applicationId,
        [FromBody] ApplicationCreationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EditApplicationCommand(applicationId, request), cancellationToken);
        var response = new ApiResponse<ApplicationResponseDto>
        {
            Success = result != null,
            Data = result,
            Message = result != null ? "Success" : "Failed to edit application."
        };
        return Ok(response);
    }

    // GET /api/Application/{applicationId}
    [HttpGet("{applicationId:guid}")]
    [ProducesResponseType(typeof(ApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationByIdAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApplicationByIdQuery(applicationId), cancellationToken);
        if (result is null)
            return NotFound();
        var response = new ApiResponse<ApplicationResponseDto>
        {
            Success = true,
            Data = result
        };
        return Ok(response);
    }

    // GET /api/Application/by-code/{code}
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApplicationByCodeQuery(code), cancellationToken);
        if (result is null)
            return NotFound();
        var response = new ApiResponse<ApplicationResponseDto>
        {
            Success = true,
            Data = result
        };
        return Ok(response);
    }

    // GET /api/Application/get-all
    [HttpGet("get-all")]
    [ProducesResponseType(typeof(List<ApplicationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllApplicationsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllApplicationsQuery(), cancellationToken);
        var response = new ApiResponse<List<ApplicationResponseDto>>
        {
            Success = true,
            Data = result
        };
        return Ok(response);
    }

    // GET /api/Application/by-type/{applicationType}
    [HttpGet("by-type/{applicationType:int}")]
    [ProducesResponseType(typeof(List<ApplicationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationsByTypeAsync(int applicationType, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApplicationsByTypeQuery(applicationType), cancellationToken);
        var response = new ApiResponse<List<ApplicationResponseDto>>
        {
            Success = true,
            Data = result
        };
        return Ok(response);
    }

    // GET /api/Application/{applicationId}/users
    [HttpGet("{applicationId:guid}/users")]
    [ProducesResponseType(typeof(List<ApplicationUserResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationUsersAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApplicationUsersQuery(applicationId), cancellationToken);
        var response = new ApiResponse<List<ApplicationUserResponseDto>>
        {
            Success = true,
            Data = result
        };
        return Ok(response);
    }

    // POST /api/Application/invite-user?invitedBy={guid}
    [HttpPost("invite-user")]
    [ProducesResponseType(typeof(InvitationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InviteUserAsync(
        [FromQuery] Guid invitedBy,
        [FromBody] InviteUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new InviteUserCommand(invitedBy, request), cancellationToken);
        var response = new ApiResponse<InvitationResponseDto>
        {
            Success = result != null,
            Data = result,
            Message = result != null ? "Success" : "Failed to invite user."
        };
        return Ok(response);
    }

    // POST /api/Application/{applicationId}/users/{userId}/assign?roleId={guid}&assignedBy={guid}
    [HttpPost("{applicationId:guid}/users/{userId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignUserToApplicationAsync(
        Guid applicationId,
        Guid userId,
        [FromQuery] Guid roleId,
        [FromQuery] Guid assignedBy,
        CancellationToken cancellationToken)
    {
        await sender.Send(new AssignUserCommand(applicationId, userId, roleId, assignedBy), cancellationToken);
        return Ok();
    }

    // POST /api/Application/{applicationId}/users/{userId}/mapping?assignedBy={guid}
    [HttpPost("{applicationId:guid}/users/{userId:guid}/mapping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddApplicationUserMappingAsync(
        Guid applicationId,
        Guid userId,
        [FromQuery] Guid assignedBy,
        CancellationToken cancellationToken)
    {
        await sender.Send(new AddUserMappingCommand(applicationId, userId, assignedBy), cancellationToken);
        return Ok();
    }

    // DELETE /api/Application/{applicationId}/users/{userId}?removedBy={guid}
    [HttpDelete("{applicationId:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserFromApplicationAsync(
        Guid applicationId,
        Guid userId,
        [FromQuery] Guid removedBy,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveUserCommand(applicationId, userId, removedBy), cancellationToken);
        return Ok();
    }

    // DELETE /api/Application/{applicationId}
    [HttpDelete("{applicationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteApplicationCommand(applicationId), cancellationToken);
        return NoContent();
    }

    // PATCH /api/Application/{applicationId}/status?modifiedBy={guid}
    [HttpPatch("{applicationId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleApplicationStatusAsync(
        Guid applicationId,
        [FromQuery] Guid modifiedBy,
        [FromBody] ToggleStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleApplicationStatusCommand(applicationId, modifiedBy, request.IsActive), cancellationToken);
        var response = new ApiResponse
        {
            Success = true,
            Message = request.IsActive ? "Application activated successfully" : "Application deactivated successfully"
        };
        return Ok(response);
    }

    // PATCH /api/Application/{applicationId}/users/{userId}/status?modifiedBy={guid}
    [HttpPatch("{applicationId:guid}/users/{userId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleUserApplicationMappingAsync(
        Guid applicationId,
        Guid userId,
        [FromQuery] Guid modifiedBy,
        [FromBody] ToggleStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleUserApplicationMappingCommand(applicationId, userId, modifiedBy, request.IsActive), cancellationToken);
        var response = new ApiResponse
        {
            Success = true,
            Message = request.IsActive ? "User mapping activated successfully" : "User mapping deactivated successfully"
        };
        return Ok(response);
    }

    // PUT /api/Application/{applicationId}/users/{userId}/role?modifiedBy={guid}
    [HttpPut("{applicationId:guid}/users/{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeUserRoleAsync(
        Guid applicationId,
        Guid userId,
        [FromQuery] Guid modifiedBy,
        [FromBody] ChangeUserRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeUserRoleCommand(applicationId, userId, modifiedBy, request.NewRoleId), cancellationToken);
        var response = new ApiResponse
        {
            Success = true,
            Message = "User role changed successfully"
        };
        return Ok(response);
    }
}
