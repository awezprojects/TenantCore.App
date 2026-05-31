namespace TenantCore.Web.Client.Services;

public class ClinicAuthorizationHandler(ClinicContextService clinicContext) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only inject from shared context if caller hasn't already set the header explicitly.
        // This lets per-clinic stats calls on the landing page pass their own app ID
        // without the shared (Guid.Empty at that point) context overwriting it.
        if (!request.Headers.Contains("X-Application-Id")
            && clinicContext.SelectedApplicationId != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation(
                "X-Application-Id", clinicContext.SelectedApplicationId.ToString());
            if (!string.IsNullOrWhiteSpace(clinicContext.SelectedApplicationName))
                request.Headers.TryAddWithoutValidation(
                    "X-Application-Name", clinicContext.SelectedApplicationName);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
