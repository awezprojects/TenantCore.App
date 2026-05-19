using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Commands;

public sealed record CreateMedicineDosageFormCommand(string Name, string? Description) : IRequest<MedicineDosageFormDto>;
