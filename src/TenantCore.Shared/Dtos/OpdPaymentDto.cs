using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class OpdPaymentDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid OpdRegistrationId { get; init; }
    public decimal VisitFee { get; init; }
    public decimal ParticularsTotal { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal Discount { get; init; }
    public decimal FinalAmount { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public DateTime? AmountReceivedAt { get; init; }
    public Guid? ReceivedByUserId { get; init; }
    public Guid? CounterSessionId { get; init; }
}

public sealed record EnsureOpdPaymentRequest
{
    public Guid OpdRegistrationId { get; init; }
    public Guid DoctorProfileId { get; init; }
}

public sealed record AcceptOpdPaymentRequest
{
    public Guid OpdRegistrationId { get; init; }
    public Guid? CounterSessionId { get; init; }
}

public sealed record ApplyOpdDiscountRequest
{
    public Guid OpdRegistrationId { get; init; }
    public decimal Discount { get; init; }
}

// Collects the entire bill (visit fee + all services - discount) in one transaction.
// Used only when OpdPayment.Discount > 0.
public sealed record AcceptOpdPaymentFullRequest
{
    public Guid OpdRegistrationId { get; init; }
    public Guid? CounterSessionId { get; init; }
}
