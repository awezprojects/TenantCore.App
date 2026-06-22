using MediatR;

namespace TenantCore.Application.Features.OpdPayments.Commands;

public sealed record EnsureOpdPaymentCommand(Guid OpdRegistrationId, Guid DoctorProfileId, Guid ApplicationId) : IRequest<Guid>;
