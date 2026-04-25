using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Commands;

public sealed record CreateMedicineTypeCommand(string Name, string? Description) : IRequest<MedicineTypeDto>;
