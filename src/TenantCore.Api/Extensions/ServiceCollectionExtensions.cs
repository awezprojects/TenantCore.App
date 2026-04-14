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
            // Default policy - requires authentication
            .AddPolicy(AuthPolicies.RequireAuthenticated, policy =>
                policy.RequireAuthenticatedUser())
            // Admin policy - any admin role
            .AddPolicy(AuthPolicies.RequireAdmin, policy =>
                policy.RequireRole(AppRoles.AdminRoles))
            // Clinic Admin policy
            .AddPolicy(AuthPolicies.RequireClinicAdmin, policy =>
                policy.RequireRole(AppRoles.ClinicAdmin, AppRoles.SystemAdmin))
            // School Admin policy
            .AddPolicy(AuthPolicies.RequireSchoolAdmin, policy =>
                policy.RequireRole(AppRoles.SchoolAdmin, AppRoles.SystemAdmin))
            // Management policy - admins and managers
            .AddPolicy(AuthPolicies.RequireManagement, policy =>
                policy.RequireRole(AppRoles.ManagementRoles));

        return services;
    }
}
