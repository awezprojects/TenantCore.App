namespace TenantCore.Domain.Enums;

/// <summary>Internal to the durable workflow — never exposed to Shared/client.</summary>
public enum WorkflowTaskType
{
    CreatePaymentLink = 1,
    CancelPaymentLink = 2,
    SendEmail = 3,
    ConfirmPayment = 4,
    ProvisionClinic = 5,
    ActivateSubscription = 6,
    ProcessWebhookEvent = 7
}
