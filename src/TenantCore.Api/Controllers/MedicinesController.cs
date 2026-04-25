using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Medicines.Commands;
using TenantCore.Application.Features.Medicines.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/medicines")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class MedicinesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MedicineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? brandName = null,
        [FromQuery] string? genericName = null,
        [FromQuery] Guid? medicineTypeId = null,
        [FromQuery] bool? isGeneric = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(
            new GetMedicinesQuery(page, pageSize, search, brandName, genericName, medicineTypeId, isGeneric), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetMedicineByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateMedicineCommand(
            dto.Name, dto.GenericName, dto.BrandName, dto.Description,
            dto.Composition, dto.Composition2, dto.Dosage, dto.Form,
            dto.Manufacturer, dto.IsGeneric, dto.PackSize, dto.Uses,
            dto.SideEffects, dto.Contraindications, dto.Storage, dto.MedicineTypeId), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateMedicineCommand(
            id, dto.Name, dto.GenericName, dto.BrandName, dto.Description,
            dto.Composition, dto.Composition2, dto.Dosage, dto.Form,
            dto.Manufacturer, dto.IsGeneric, dto.PackSize, dto.Uses,
            dto.SideEffects, dto.Contraindications, dto.Storage, dto.IsActive, dto.MedicineTypeId), ct));
}
