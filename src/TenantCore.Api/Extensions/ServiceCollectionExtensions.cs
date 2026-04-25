using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TenantCore.Shared.Authorization;

namespace TenantCore.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
        var key = Encoding.ASCII.GetBytes(secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = !string.IsNullOrEmpty(jwtSection["Issuer"]),
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = !string.IsNullOrEmpty(jwtSection["Audience"]),
                ValidAudience = jwtSection["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                // Map the 'role' claim properly from JWT
                RoleClaimType = "role",
                NameClaimType = "email"
            };
        });

        // Add role-based authorization policies
        services.AddAuthorizationBuilder()
            // Any authenticated clinic user
            .AddPolicy(AuthPolicies.RequireAuthenticated, policy =>
                policy.RequireAuthenticatedUser())
            // Clinic Admin only (fee config, admin-level settings)
            .AddPolicy(AuthPolicies.RequireClinicAdmin, policy =>
                policy.RequireRole(AppRoles.ClinicAdmin, AppRoles.SystemAdmin))
            // Reception: register patients, create OPD/IPD registrations
            .AddPolicy(AuthPolicies.RequireReception, policy =>
                policy.RequireRole(AppRoles.ReceptionRoles))
            // Clinical: doctors + management for clinical actions (e.g. discharge)
            .AddPolicy(AuthPolicies.RequireClinical, policy =>
                policy.RequireRole(AppRoles.ClinicalRoles));

        return services;
    }
}
