using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class PrescriptionsController(ISender sender) : ClinicControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png", ".bmp" };
    private const long MaxUploadBytes = 50 * 1024 * 1024;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? doctorUserId = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetPrescriptionsQuery(GetApplicationId(), page, pageSize, search, doctorUserId, patientId, from, to), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetPrescriptionByIdQuery(id, GetApplicationId()), ct));

    [HttpGet("opd/{opdRegistrationId:guid}")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOpdId(Guid opdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetPrescriptionByOpdIdQuery(opdRegistrationId, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreatePrescriptionCommand(
            GetApplicationId(), dto.OpdRegistrationId, dto.DoctorUserId,
            dto.DoctorName, dto.NextVisitDate, dto.Diagnosis, dto.Investigations, dto.Notes,
            dto.VitalBP, dto.VitalPulse, dto.VitalTemp, dto.VitalWeight, dto.VitalSpO2, dto.VitalRR, dto.VitalSugar,
            dto.Items), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrescriptionDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdatePrescriptionCommand(
            id, GetApplicationId(), dto.NextVisitDate, dto.Diagnosis, dto.Investigations, dto.Notes,
            dto.VitalBP, dto.VitalPulse, dto.VitalTemp, dto.VitalWeight, dto.VitalSpO2, dto.VitalRR, dto.VitalSugar,
            dto.Items), ct));

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new SubmitPrescriptionCommand(id, GetApplicationId()), ct));

    [HttpPost("{id:guid}/reports")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(PrescriptionReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadReport(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (file.Length > MaxUploadBytes)
            return BadRequest("File exceeds the 50 MB limit.");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest("File type not allowed. Accepted: PDF, JPG, PNG, BMP.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var fileBytes = ms.ToArray();

        var result = await sender.Send(new UploadPrescriptionReportCommand(
            id, GetApplicationId(), file.FileName, fileBytes, ext), ct);

        return CreatedAtAction(nameof(GetById), new { id }, result);
    }
}
