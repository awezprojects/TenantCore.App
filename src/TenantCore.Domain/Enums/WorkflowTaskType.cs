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
    ProcessWebhookEvent = 7,

    /// <summary>Phase 2 — credits a clinic wallet after a <c>WalletTopUp</c> payment is confirmed.</summary>
    CreditWallet = 8,

    /// <summary>Phase 2 — marks the one-time registration fee charged after its payment is confirmed.</summary>
    RecordRegistrationFee = 9
}
