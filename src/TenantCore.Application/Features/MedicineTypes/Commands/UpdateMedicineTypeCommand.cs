using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Commands;

public sealed record UpdateMedicineTypeCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest<MedicineTypeDto>;
