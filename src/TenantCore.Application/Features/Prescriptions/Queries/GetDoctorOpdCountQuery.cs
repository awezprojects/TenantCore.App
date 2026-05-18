using MediatR;

namespace TenantCore.Application.Features.Prescriptions.Queries;

public sealed record GetDoctorOpdCountQuery(Guid ApplicationId, Guid DoctorUserId) : IRequest<int>;
