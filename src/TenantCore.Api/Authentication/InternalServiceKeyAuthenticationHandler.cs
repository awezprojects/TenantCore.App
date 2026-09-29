using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TenantCore.Api.Authentication;

/// <summary>
/// Authentication scheme "InternalService" — guards api/internal/* for the TenantCore.Admin
/// portal only. Validates X-Internal-Service-Key against InternalApi:AdminPortalKey in fixed
/// time. Deliberately not the default scheme, so every existing [Authorize] attribute using
/// JWT bearer auth is unaffected.
/// </summary>
public class InternalServiceKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<InternalApiOptions> internalApiOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "InternalService";
    private const string HeaderName = "X-Internal-Service-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = internalApiOptions.Value.AdminPortalKey;
        if (string.IsNullOrEmpty(configuredKey))
            return Task.FromResult(AuthenticateResult.Fail("InternalApi:AdminPortalKey is not configured."));

        if (!Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided))
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Internal-Service-Key header."));

        if (!FixedTimeEquals(provided.ToString(), configuredKey))
            return Task.FromResult(AuthenticateResult.Fail("Invalid service key."));

        var claims = new[] { new Claim(ClaimTypes.Role, "InternalService"), new Claim(ClaimTypes.Name, "AdminPortal") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        if (aBytes.Length != bBytes.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
