using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/ipd-registrations")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class IpdRegistrationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IpdRegistrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetIpdRegistrationsQuery(GetApplicationId(), page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdRegistrationByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateIpdRegistrationDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateIpdRegistrationCommand(
            GetApplicationId(), dto.PatientId, dto.DoctorUserId, dto.DoctorName,
            dto.WardName, dto.RoomNumber, dto.BedNumber,
            dto.InitialFee, dto.AdmissionNotes), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIpdRegistrationDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateIpdRegistrationCommand(
            id, GetApplicationId(), dto.DoctorUserId, dto.DoctorName,
            dto.WardName, dto.RoomNumber, dto.BedNumber,
            dto.Status, dto.AdmissionNotes), ct));

    [HttpPatch("{id:guid}/discharge")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Discharge(Guid id, [FromBody] DischargePatientDto dto, CancellationToken ct)
        => Ok(await sender.Send(new DischargePatientCommand(id, GetApplicationId(), dto.DischargeNotes), ct));

    private Guid GetApplicationId()
    {
        var claim = User.FindFirst("app_ids");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
