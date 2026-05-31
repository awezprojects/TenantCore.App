using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TenantCore.Api.Authorization;
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

        // Register the handler that enforces per-clinic role checks via the app_roles JWT claim.
        services.AddSingleton<IAuthorizationHandler, ClinicRoleAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            // Any authenticated clinic user — no clinic-role check needed
            .AddPolicy(AuthPolicies.RequireAuthenticated, policy =>
                policy.RequireAuthenticatedUser())
            // Clinic Admin only — checked against the active X-Application-Id clinic
            .AddPolicy(AuthPolicies.RequireClinicAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ClinicRoleRequirement(AppRoles.ClinicAdmin, AppRoles.SystemAdmin)))
            // Reception: register patients, create OPD/IPD — per-clinic check
            .AddPolicy(AuthPolicies.RequireReception, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ClinicRoleRequirement(AppRoles.ReceptionRoles)))
            // Clinical: doctors + management — per-clinic check
            .AddPolicy(AuthPolicies.RequireClinical, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ClinicRoleRequirement(AppRoles.ClinicalRoles)));

        return services;
    }
}
