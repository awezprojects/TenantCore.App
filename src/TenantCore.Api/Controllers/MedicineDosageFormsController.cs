using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/medicine-dosage-forms")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class MedicineDosageFormsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MedicineDosageFormDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetMedicineDosageFormsQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MedicineDosageFormDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetMedicineDosageFormByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(MedicineDosageFormDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineDosageFormDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateMedicineDosageFormCommand(dto.Name, dto.Description), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(MedicineDosageFormDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineDosageFormDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateMedicineDosageFormCommand(id, dto.Name, dto.Description, dto.IsActive), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteMedicineDosageFormCommand(id), ct);
        return NoContent();
    }
}
