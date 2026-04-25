using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Commands;

public sealed record UpdateMedicineCommand(
    Guid Id,
    string Name,
    string? GenericName,
    string? BrandName,
    string? Description,
    string? Composition,
    string? Composition2,
    string? Dosage,
    string? Form,
    string? Manufacturer,
    bool IsGeneric,
    string? PackSize,
    string? Uses,
    string? SideEffects,
    string? Contraindications,
    string? Storage,
    bool IsActive,
    Guid? MedicineTypeId) : IRequest<MedicineDto>;
