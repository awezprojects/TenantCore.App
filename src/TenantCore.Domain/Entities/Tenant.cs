using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Domain.Entities;

public class Tenant : AuditableEntity
{
    private Tenant() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Subdomain { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }

    public static Tenant Create(
        string name,
        string subdomain,
        string? description = null,
        string? contactEmail = null,
        string? contactPhone = null,
        DateTime? subscriptionExpiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Tenant name cannot be empty.");
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new DomainValidationException("Tenant subdomain cannot be empty.");

        return new Tenant
        {
            Name = name.Trim(),
            Subdomain = subdomain.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            ContactEmail = contactEmail?.Trim(),
            ContactPhone = contactPhone?.Trim(),
            SubscriptionExpiresAt = subscriptionExpiresAt
        };
    }

    public void Update(
        string name,
        string subdomain,
        string? description,
        string? contactEmail,
        string? contactPhone,
        DateTime? subscriptionExpiresAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Tenant name cannot be empty.");
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new DomainValidationException("Tenant subdomain cannot be empty.");

        Name = name.Trim();
        Subdomain = subdomain.Trim().ToLowerInvariant();
        Description = description?.Trim();
        ContactEmail = contactEmail?.Trim();
        ContactPhone = contactPhone?.Trim();
        SubscriptionExpiresAt = subscriptionExpiresAt;
        SetUpdatedAt();
    }

    public void Activate() { IsActive = true; SetUpdatedAt(); }
    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
}
