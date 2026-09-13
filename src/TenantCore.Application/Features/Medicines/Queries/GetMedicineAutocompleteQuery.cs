using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Queries;

public sealed record GetMedicineAutocompleteQuery(string Name, Guid ApplicationId, int Limit = 5) : IRequest<IEnumerable<MedicineDto>>;
