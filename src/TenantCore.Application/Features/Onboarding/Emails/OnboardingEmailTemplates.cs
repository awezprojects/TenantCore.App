using System.Net;
using System.Text.Json;

namespace TenantCore.Application.Features.Onboarding.Emails;

/// <summary>
/// Static HTML builders for every onboarding/payment email, plus the (template id, recipient,
/// model) JSON payload convention a SendEmail WorkflowTask carries. All user-supplied values are
/// HTML-encoded before being placed into markup.
/// </summary>
public static class OnboardingEmailTemplates
{
    // Deliberately no camelCase naming policy: Render()'s Str() helper reads model properties by
    // their exact PascalCase C# member names (ClinicName, PlanName, ...), so the payload's model
    // object must round-trip with those names unchanged.
    private static readonly JsonSerializerOptions JsonOptions = new();

    public static string BuildPayload(string templateId, string to, object model) =>
        JsonSerializer.Serialize(new { template = templateId, to, model }, JsonOptions);

    public static (string To, string Subject, string HtmlBody) Render(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        var template = root.TryGetProperty("template", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        var to = root.TryGetProperty("to", out var toEl) ? toEl.GetString() ?? string.Empty : string.Empty;
        var model = root.TryGetProperty("model", out var m) ? m : default;

        var (subject, body) = template switch
        {
            "RequestReceived" => RequestReceived(model),
            "OpsNewRequest" => OpsNewRequest(model),
            "PaymentLink" => PaymentLink(model),
            "ClinicReady" => ClinicReady(model),
            "Rejected" => Rejected(model),
            "LinkExpired" => LinkExpired(model),
            "RenewalPaymentLink" => RenewalPaymentLink(model),
            "AssignedPaymentLink" => AssignedPaymentLink(model),
            "RenewalActivated" => RenewalActivated(model),
            "SubscriptionGranted" => SubscriptionGranted(model),
            "SilentWebhookAlert" => SilentWebhookAlert(model),
            _ => ("Notification", "<p>Notification</p>")
        };

        return (to, subject, body);
    }

    /// <summary>
    /// Same payload SendEmailTaskHandler already carries, but for the "publish to Service Bus,
    /// let the notification consumer render the template" flow instead of building HTML locally.
    /// Reuses Render()'s per-template subject text so both paths never disagree.
    /// </summary>
    public static (string To, string Template, string Subject, Dictionary<string, string> TemplateData) RenderForQueue(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        var template = root.TryGetProperty("template", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        var to = root.TryGetProperty("to", out var toEl) ? toEl.GetString() ?? string.Empty : string.Empty;
        var model = root.TryGetProperty("model", out var m) ? m : default;

        var (_, subject, _) = Render(payloadJson);

        var templateData = new Dictionary<string, string>();
        if (model.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in model.EnumerateObject())
                templateData[property.Name] = Str(model, property.Name);
        }

        return (to, template, subject, templateData);
    }

    private static (string, string) RequestReceived(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var requesterName = Str(model, "RequesterName");
        var body = $"""
            <p>Hi {Enc(requesterName)},</p>
            <p>We've received your request to set up <strong>{Enc(clinicName)}</strong>. Our team will review it shortly.</p>
            <p>You can check the status of your request any time from your dashboard.</p>
            """;
        return ("We've received your clinic request", body);
    }

    private static (string, string) OpsNewRequest(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var doctorName = Str(model, "DoctorName");
        var requesterEmail = Str(model, "RequesterEmail");
        var body = $"""
            <p>New clinic onboarding request:</p>
            <ul>
                <li><strong>Clinic:</strong> {Enc(clinicName)}</li>
                <li><strong>Doctor:</strong> {Enc(doctorName)}</li>
                <li><strong>Requester email:</strong> {Enc(requesterEmail)}</li>
            </ul>
            <p>Review it in the admin portal.</p>
            """;
        return ($"New clinic request — {clinicName}", body);
    }

    private static (string, string) PaymentLink(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var planName = Str(model, "PlanName");
        var amount = Str(model, "Amount");
        var currency = Str(model, "Currency");
        var url = Str(model, "PaymentLinkUrl");
        var expiresAt = Str(model, "ExpiresAt");
        var body = $"""
            <p>Your clinic request for <strong>{Enc(clinicName)}</strong> has been approved for the <strong>{Enc(planName)}</strong> plan.</p>
            <p>Amount due: <strong>{Enc(currency)} {Enc(amount)}</strong></p>
            <p><a href="{Enc(url)}">Pay now</a> (link expires {Enc(expiresAt)})</p>
            <p>Once payment is confirmed, your clinic will be set up automatically.</p>
            """;
        return ("Complete your payment to activate your clinic", body);
    }

    private static (string, string) ClinicReady(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var body = $"""
            <p>Great news — <strong>{Enc(clinicName)}</strong> is ready.</p>
            <p>Open your dashboard to start using it.</p>
            """;
        return ("Your clinic is ready", body);
    }

    private static (string, string) Rejected(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var reason = Str(model, "Reason");
        var body = $"""
            <p>Your request for <strong>{Enc(clinicName)}</strong> was not approved.</p>
            <p><strong>Reason:</strong> {Enc(reason)}</p>
            <p>You're welcome to submit a new request at any time.</p>
            """;
        return ("Update on your clinic request", body);
    }

    private static (string, string) LinkExpired(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var body = $"""
            <p>The payment link for <strong>{Enc(clinicName)}</strong> has expired.</p>
            <p>Please submit a new clinic request to try again.</p>
            """;
        return ("Your payment link has expired", body);
    }

    private static (string, string) RenewalPaymentLink(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var planName = Str(model, "PlanName");
        var amount = Str(model, "Amount");
        var currency = Str(model, "Currency");
        var url = Str(model, "PaymentLinkUrl");
        var body = $"""
            <p>Renew <strong>{Enc(clinicName)}</strong> on the <strong>{Enc(planName)}</strong> plan.</p>
            <p>Amount due: <strong>{Enc(currency)} {Enc(amount)}</strong></p>
            <p><a href="{Enc(url)}">Pay now</a></p>
            """;
        return ("Renew your subscription", body);
    }

    /// <summary>A link an internal admin sent to an existing clinic, rather than one the clinic asked for.</summary>
    private static (string, string) AssignedPaymentLink(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var planName = Str(model, "PlanName");
        var amount = Str(model, "Amount");
        var currency = Str(model, "Currency");
        var url = Str(model, "PaymentLinkUrl");
        var body = $"""
            <p>We've prepared a payment link for <strong>{Enc(clinicName)}</strong> on the <strong>{Enc(planName)}</strong> plan.</p>
            <p>Amount due: <strong>{Enc(currency)} {Enc(amount)}</strong></p>
            <p><a href="{Enc(url)}">Pay now</a></p>
            <p>Your new term starts automatically once payment is confirmed — if your clinic is already
               covered, it begins the moment the current term ends.</p>
            """;
        return ("Your CloudClinic payment link", body);
    }

    private static (string, string) SubscriptionGranted(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var planName = Str(model, "PlanName");
        var startDate = Str(model, "StartDate");
        var endDate = Str(model, "EndDate");
        var body = $"""
            <p><strong>{Enc(clinicName)}</strong> has been given the <strong>{Enc(planName)}</strong> plan
               at no charge.</p>
            <p>It runs from {Enc(startDate)} to {Enc(endDate)}.</p>
            """;
        return ("A subscription has been added to your clinic", body);
    }

    private static (string, string) RenewalActivated(JsonElement model)
    {
        var clinicName = Str(model, "ClinicName");
        var startDate = Str(model, "StartDate");
        var endDate = Str(model, "EndDate");

        // StartDate is absent on payloads queued before it was added — fall back to the old wording.
        var period = string.IsNullOrEmpty(startDate)
            ? $"valid through {Enc(endDate)}"
            : $"valid from {Enc(startDate)} through {Enc(endDate)}";

        var body = $"""
            <p><strong>{Enc(clinicName)}</strong> has been renewed, {period}.</p>
            """;
        return ("Your subscription is renewed", body);
    }

    private static (string, string) SilentWebhookAlert(JsonElement model)
    {
        var paymentId = Str(model, "paymentId");
        var body = $"""
            <p>Payment <strong>{Enc(paymentId)}</strong> was confirmed by the 15-minute reconciliation sweep, not by a Razorpay webhook.</p>
            <p>This usually means Razorpay has disabled the webhook after repeated failed deliveries. Check and re-enable it in the Razorpay dashboard.</p>
            """;
        return ("Action needed: Razorpay webhook may be disabled", body);
    }

    private static string Str(JsonElement model, string propertyName) =>
        model.ValueKind == JsonValueKind.Object && model.TryGetProperty(propertyName, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                _ => value.ToString()
            }
            : string.Empty;

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
