using MediatR;

namespace TenantCore.Application.Features.MedicineDosageForms.Commands;

public sealed record DeleteMedicineDosageFormCommand(Guid Id) : IRequest;
