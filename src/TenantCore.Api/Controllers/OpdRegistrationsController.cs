using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/opd-registrations")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class OpdRegistrationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OpdRegistrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetOpdRegistrationsQuery(GetApplicationId(), page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OpdRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetOpdRegistrationByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdRegistrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOpdRegistrationDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateOpdRegistrationCommand(
            GetApplicationId(), dto.PatientId, dto.DoctorUserId,
            dto.DoctorName, dto.Fee, dto.Notes,
            dto.Weight, dto.BloodPressure, dto.PulseRate, dto.OxygenSaturation), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdRegistrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOpdRegistrationDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateOpdRegistrationCommand(
            id, GetApplicationId(), dto.DoctorUserId, dto.DoctorName,
            dto.Fee, dto.Status, dto.Notes), ct));

    private Guid GetApplicationId()
    {
        var claim = User.FindFirst("app_ids");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
