using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Queries;

public sealed record GetPrescriptionsQuery(
    Guid ApplicationId,
    int Page,
    int PageSize,
    string? Search,
    Guid? DoctorUserId,
    Guid? PatientId,
    DateTime? From,
    DateTime? To) : IRequest<PagedResult<PrescriptionDto>>;
