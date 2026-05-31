using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/dosage-remarks")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class DosageRemarksController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DosageRemarkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] MedicineFormType? form = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetDosageRemarksQuery(GetApplicationId(), page, pageSize, form), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DosageRemarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetDosageRemarkByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(DosageRemarkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDosageRemarkDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateDosageRemarkCommand(
            GetApplicationId(), dto.MedicineForm, dto.RemarkEnglish, dto.RemarkHindi, dto.RemarkMarathi), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(DosageRemarkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDosageRemarkDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateDosageRemarkCommand(
            id, GetApplicationId(), dto.MedicineForm, dto.RemarkEnglish, dto.RemarkHindi, dto.RemarkMarathi, dto.IsActive), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteDosageRemarkCommand(id, GetApplicationId()), ct);
        return NoContent();
    }
}
