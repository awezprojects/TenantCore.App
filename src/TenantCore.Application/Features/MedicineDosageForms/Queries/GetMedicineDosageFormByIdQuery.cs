using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Queries;

public sealed record GetMedicineDosageFormByIdQuery(Guid Id) : IRequest<MedicineDosageFormDto>;
