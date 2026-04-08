using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Translators;

public static class TenantTranslator
{
    public static TenantDto ToDto(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Subdomain = tenant.Subdomain,
        Description = tenant.Description,
        IsActive = tenant.IsActive,
        ContactEmail = tenant.ContactEmail,
        ContactPhone = tenant.ContactPhone,
        SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
        CreatedAt = tenant.CreatedAt,
        UpdatedAt = tenant.UpdatedAt
    };
}
