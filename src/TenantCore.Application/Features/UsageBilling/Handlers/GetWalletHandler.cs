using MediatR;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.UsageBilling.Queries;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// The wallet screen's single read: balance, threshold and the newest ledger page. A clinic that has
/// never topped up has no wallet row yet — that is not an error, it is a zero balance at the
/// configured threshold, and the first credit creates the row.
/// </summary>
public sealed class GetWalletHandler(
    IClinicAccountRepository accountRepository,
    IClinicWalletRepository walletRepository,
    IClinicWalletTransactionRepository walletTransactionRepository,
    IOptions<UsageBillingOptions> options)
    : IRequestHandler<GetWalletQuery, ClinicWalletDto>
{
    /// <summary>Enough history to explain the balance without paging; the ledger keeps growing behind it.</summary>
    private const int LedgerPageSize = 50;

    private readonly UsageBillingOptions _options = options.Value;

    public async Task<ClinicWalletDto> Handle(GetWalletQuery query, CancellationToken ct)
    {
        var mode = await accountRepository.GetBillingModeAsync(query.ApplicationId, ct);
        if (mode == BillingMode.Subscription)
            throw new ErrorCodeException(403, "subscription_billing_not_applicable",
                "Wallet is not available for subscription clinics.");

        var ledger = await walletTransactionRepository.GetForClinicAsync(query.ApplicationId, LedgerPageSize, ct);
        var wallet = await walletRepository.GetByApplicationIdNoTrackingAsync(query.ApplicationId, ct);

        // A clinic that has never been credited has no row. Reporting a zero balance at the configured
        // threshold is honest and actionable — it is exactly the state that must be topped up before a
        // pay-as-you-go operation can be charged for.
        if (wallet is null)
            return new ClinicWalletDto
            {
                Balance = 0m,
                LowBalanceThreshold = _options.DefaultLowBalanceThreshold,
                IsLow = _options.DefaultLowBalanceThreshold >= 0m,
                Currency = _options.Currency,
                Transactions = ledger.Select(UsageBillingTranslator.ToDto).ToList()
            };

        return UsageBillingTranslator.ToDto(wallet, ledger);
    }
}

