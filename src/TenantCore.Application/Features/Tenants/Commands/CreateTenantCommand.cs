using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Commands;

public record CreateTenantCommand(
    string Name,
    string Subdomain,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    DateTime? SubscriptionExpiresAt) : IRequest<TenantDto>;
