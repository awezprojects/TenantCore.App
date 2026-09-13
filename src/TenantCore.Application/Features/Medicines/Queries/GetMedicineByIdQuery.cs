using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Queries;

public sealed record GetMedicineByIdQuery(Guid Id, Guid ApplicationId) : IRequest<MedicineDto>;
