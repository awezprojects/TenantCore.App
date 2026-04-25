using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/medicine-types")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class MedicineTypesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MedicineTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetMedicineTypesQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MedicineTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetMedicineTypeByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(MedicineTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineTypeDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateMedicineTypeCommand(dto.Name, dto.Description), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(MedicineTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineTypeDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateMedicineTypeCommand(id, dto.Name, dto.Description, dto.IsActive), ct));
}
