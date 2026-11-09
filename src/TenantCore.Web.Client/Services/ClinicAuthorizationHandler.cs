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

        // DEF-FB-03: tells the API the user explicitly chose to view this clinic as "Doctor"
        // this session (e.g. a user who is both Doctor and Clinic Admin, via "🩺 Doctor Panel"
        // on the clinic picker) so endpoints that would otherwise honor their admin-level role
        // can scope down to just their own data instead — see ClinicControllerBase.IsDoctorPanelView().
        if (!request.Headers.Contains("X-View-Scope") && clinicContext.ActivePanel == "Doctor")
            request.Headers.TryAddWithoutValidation("X-View-Scope", "Doctor");

        return base.SendAsync(request, cancellationToken);
    }
}
