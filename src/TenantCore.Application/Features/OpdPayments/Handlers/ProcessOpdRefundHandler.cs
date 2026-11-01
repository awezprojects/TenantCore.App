using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class ProcessOpdRefundHandler(
    IOpdPaymentRepository repository,
    IClinicUsageRecordRepository usageRecordRepository,
    IClinicWalletRepository walletRepository,
    IClinicWalletTransactionRepository walletTransactionRepository,
    ILogger<ProcessOpdRefundHandler> logger)
    : IRequestHandler<ProcessOpdRefundCommand, OpdPaymentDto>
{
    public async Task<OpdPaymentDto> Handle(ProcessOpdRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByOpdRegistrationIdAsync(
            request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            throw new NotFoundException(nameof(OpdPayment), request.Request.OpdRegistrationId);

        payment.ProcessRefund(request.RefundedByUserId);
        repository.Update(payment);

        await ReverseUsageChargeAsync(request.ApplicationId, request.Request.OpdRegistrationId,
            request.RefundedByUserId, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        return OpdPaymentTranslator.ToDto(payment);
    }

    private async Task ReverseUsageChargeAsync(
        Guid applicationId, Guid opdRegistrationId, Guid userId, CancellationToken ct)
    {
        var usageRecord = await usageRecordRepository.GetBySourceAsync(
            applicationId, BillingItem.OpdRegistration, opdRegistrationId, ct);

        if (usageRecord is null || usageRecord.IsFree || usageRecord.Amount <= 0m)
            return;

        var wallet = await walletRepository.GetByApplicationIdAsync(applicationId, ct);
        if (wallet is null)
            return;

        var utcNow = DateTime.UtcNow;
        wallet.Credit(usageRecord.Amount, utcNow);

        var ledgerRow = ClinicWalletTransaction.CreateUsageRefundCredit(
            applicationId, usageRecord.Amount, wallet.Balance, usageRecord.Id,
            $"Refund — {BillingItem.OpdRegistration}", utcNow, userId);

        await walletTransactionRepository.AddAsync(ledgerRow, ct);

        logger.LogInformation(
            "Reversed usage charge for OPD {OpdRegistrationId} in clinic {ApplicationId}: credited {Amount}, balance now {Balance}.",
            opdRegistrationId, applicationId, usageRecord.Amount, wallet.Balance);
    }
}
