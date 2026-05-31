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
public class PatientsController(ISender sender) : ClinicControllerBase
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
            dto.Gender, dto.PhoneNumber, dto.Email, dto.AadhaarNumber, dto.PhotoUrl,
            dto.Address, dto.BloodGroup, dto.EmergencyContactName, dto.EmergencyContactPhone,
            dto.KnownAllergies, dto.MedicalHistory, CanViewFullAadhaar()), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdatePatientCommand(
            id, GetApplicationId(), dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.PhoneNumber, dto.Email, dto.AadhaarNumber, dto.PhotoUrl,
            dto.Address, dto.BloodGroup, dto.EmergencyContactName, dto.EmergencyContactPhone,
            dto.KnownAllergies, dto.MedicalHistory, CanViewFullAadhaar()), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeletePatientCommand(id, GetApplicationId()), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/upload-photo")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile photo, CancellationToken ct)
    {
        if (photo is null || photo.Length == 0)
            return BadRequest("No photo file provided.");

        if (photo.Length > 5 * 1024 * 1024)
            return BadRequest("Photo file must not exceed 5 MB.");

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest("Only JPG, PNG, and WEBP images are allowed.");

        await using var stream = photo.OpenReadStream();
        var url = await sender.Send(
            new UploadPatientPhotoCommand(id, GetApplicationId(), GetApplicationName(), stream, photo.FileName, photo.ContentType), ct);

        return Ok(new { url });
    }

    private bool CanViewFullAadhaar() =>
        AppRoles.ReceptionRoles.Any(User.IsInRole);
}
