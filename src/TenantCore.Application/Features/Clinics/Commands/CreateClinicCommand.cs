using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Clinics.Commands;

public sealed record CreateClinicCommand(CreateClinicRequestDto Request) : IRequest<ApplicationResponseDto>;
