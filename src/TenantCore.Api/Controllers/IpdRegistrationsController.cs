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
public class IpdRegistrationsController(ISender sender) : ClinicControllerBase
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
            dto.BedId, dto.InitialFee, dto.AdmissionNotes), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // Doctor/notes only — status and bed move through their own dedicated actions below.
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIpdRegistrationDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateIpdRegistrationCommand(
            id, GetApplicationId(), dto.DoctorUserId, dto.DoctorName, dto.AdmissionNotes), ct));

    [HttpPatch("{id:guid}/transfer-bed")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransferBed(Guid id, [FromBody] TransferBedRequest request, CancellationToken ct)
        => Ok(await sender.Send(new TransferBedCommand(
            id, GetApplicationId(), request.NewBedId, GetCurrentUserId(), request.Notes,
            request.ManualChargeAmount, request.ManualChargeReason), ct));

    // DEF-IPD-16: lets the Shift dialog show real numbers before the user commits.
    [HttpGet("{id:guid}/transfer-bed/preview")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(TransferBedPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewTransferBed(Guid id, [FromQuery] Guid newBedId, CancellationToken ct)
        => Ok(await sender.Send(new PreviewTransferBedQuery(id, GetApplicationId(), newBedId), ct));

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelIpdRegistrationRequest request, CancellationToken ct)
        => Ok(await sender.Send(new CancelIpdRegistrationCommand(id, GetApplicationId(), request.Reason, GetCurrentUserId()), ct));

    [HttpGet("{id:guid}/bed-history")]
    [ProducesResponseType(typeof(IEnumerable<IpdBedTransferHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBedHistory(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdBedTransferHistoryQuery(id, GetApplicationId()), ct));

    // Reception can now discharge end-to-end (confirmed) — was RequireClinical, which excluded
    // Receptionist and left the report's "reception likely cannot discharge" gap open.
    [HttpPatch("{id:guid}/discharge")]
    [Authorize(Policy = AuthPolicies.RequireClinicStaff)]
    [ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Discharge(Guid id, [FromBody] DischargePatientDto dto, CancellationToken ct)
        => Ok(await sender.Send(new DischargePatientCommand(id, GetApplicationId(), dto.DischargeNotes), ct));
}
