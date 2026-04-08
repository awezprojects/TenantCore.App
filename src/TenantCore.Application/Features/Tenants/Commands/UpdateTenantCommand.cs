using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Commands;

public record UpdateTenantCommand(
    Guid Id,
    string Name,
    string Subdomain,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    DateTime? SubscriptionExpiresAt) : IRequest<TenantDto>;
