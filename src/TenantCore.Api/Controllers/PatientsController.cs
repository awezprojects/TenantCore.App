using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class PatientsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PatientDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetPatientsQuery(GetApplicationId(), page, pageSize, search, CanViewFullAadhaar()), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetPatientByIdQuery(id, GetApplicationId(), CanViewFullAadhaar()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreatePatientDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterPatientCommand(
            GetApplicationId(), dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.PhoneNumber, dto.Email,
            dto.AadhaarNumber, dto.PhotoUrl, dto.Address, CanViewFullAadhaar()), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdatePatientCommand(
            id, GetApplicationId(), dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.PhoneNumber, dto.Email,
            dto.AadhaarNumber, dto.PhotoUrl, dto.Address, CanViewFullAadhaar()), ct));

    private Guid GetApplicationId()
    {
        var claim = User.FindFirst("app_ids");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    private bool CanViewFullAadhaar() =>
        AppRoles.ReceptionRoles.Any(User.IsInRole);
}
