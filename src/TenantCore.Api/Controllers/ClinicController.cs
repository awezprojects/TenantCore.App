using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Clinics.Commands;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ClinicController(ISender sender) : ControllerBase
{
    // POST /api/Clinic
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApplicationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateClinic(
        [FromBody] CreateClinicRequestDto request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateClinicCommand(request), ct);
        return Ok(new ApiResponse<ApplicationResponseDto>
        {
            Success = result != null,
            Data = result,
            Message = result != null ? "Clinic created successfully." : "Failed to create clinic."
        });
    }

    // GET /api/Clinic/dashboard
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<List<ClinicDashboardItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetClinicDashboard(CancellationToken ct)
    {
        var result = await sender.Send(new GetClinicDashboardQuery(), ct);
        return Ok(new ApiResponse<List<ClinicDashboardItemDto>>
        {
            Success = true,
            Data = result
        });
    }
}
