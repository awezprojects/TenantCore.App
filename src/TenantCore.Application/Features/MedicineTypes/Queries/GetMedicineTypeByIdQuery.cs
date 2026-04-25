using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Queries;

public sealed record GetMedicineTypeByIdQuery(Guid Id) : IRequest<MedicineTypeDto>;
