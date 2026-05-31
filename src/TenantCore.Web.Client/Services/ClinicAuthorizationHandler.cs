namespace TenantCore.Web.Client.Services;

public class ClinicAuthorizationHandler(ClinicContextService clinicContext) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (clinicContext.SelectedApplicationId != Guid.Empty)
            request.Headers.TryAddWithoutValidation(
                "X-Application-Id", clinicContext.SelectedApplicationId.ToString());

        return base.SendAsync(request, cancellationToken);
    }
}
