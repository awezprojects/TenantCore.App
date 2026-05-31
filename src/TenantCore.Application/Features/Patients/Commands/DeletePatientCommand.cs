using MediatR;

namespace TenantCore.Application.Features.Patients.Commands;

public sealed record DeletePatientCommand(Guid Id, Guid ApplicationId) : IRequest;
