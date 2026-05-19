using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Commands;

public sealed record UpdateMedicineDosageFormCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest<MedicineDosageFormDto>;
