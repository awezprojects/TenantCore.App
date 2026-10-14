using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using TenantCore.Api.Controllers.Internal;

namespace TenantCore.Api.Tests.Controllers;

/// <summary>
/// The TenantCore.Admin portal calls these routes by hand — there is no shared typed client — so the
/// route template and the HTTP verb together are a cross-repo contract. Renaming one side without the
/// other is a silent 404 in production (exactly what happened to <c>billing-mode</c> /
/// <c>wallet/top-up</c> before this test existed). This locks the surface the portal depends on.
/// </summary>
public class InternalClinicsRouteContractTests
{
    private static string? BaseRoute =>
        typeof(InternalClinicsController).GetCustomAttribute<RouteAttribute>()?.Template;

    private static IReadOnlyList<(string Verb, string Template)> DeclaredActions()
    {
        var actions = new List<(string Verb, string Template)>();

        foreach (var method in typeof(InternalClinicsController)
                     .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var attribute in method.GetCustomAttributes<HttpMethodAttribute>())
            {
                if (attribute.Template is not { } template)
                    continue;

                foreach (var verb in attribute.HttpMethods)
                    actions.Add((verb, template));
            }
        }

        return actions;
    }

    [Fact]
    public void BaseRoute_IsTheClinicScopedInternalRoute()
        => BaseRoute.Should().Be("api/internal/clinics/{applicationId:guid}");

    [Theory]
    // ---- registration-fee-and-usage-billing ----
    [InlineData("POST", "billing")]
    [InlineData("PUT", "usage-rates")]
    [InlineData("POST", "wallet/topup")]
    [InlineData("POST", "wallet/credit")]
    [InlineData("POST", "registration-fee/send-link")]
    [InlineData("POST", "registration-fee/waive")]
    // ---- clinic-subscription-admin-controls ----
    [InlineData("POST", "suspend")]
    [InlineData("POST", "reactivate")]
    [InlineData("POST", "subscriptions/grant")]
    [InlineData("POST", "subscriptions/payment-link")]
    [InlineData("POST", "subscriptions/{subscriptionId:guid}/cancel-upcoming")]
    [InlineData("POST", "payments/{paymentId:guid}/cancel-link")]
    [InlineData("PUT", "plan-visibility")]
    [InlineData("POST", "plan-offers")]
    [InlineData("POST", "plan-offers/{offerId:guid}/withdraw")]
    public void Route_MatchesThePortalContract(string verb, string template)
        => DeclaredActions().Should().Contain((verb, template),
            $"TenantCore.Admin calls {verb} {template}; changing one side alone is a 404");

    [Fact]
    public void TheSurfaceHasExactlyTheRoutesThePortalKnows()
    {
        // 15 actions today. Adding one is fine, but it must be a deliberate change to the portal
        // contract as well — so this count is bumped consciously, never silently.
        DeclaredActions().Should().HaveCount(15);
    }
}
