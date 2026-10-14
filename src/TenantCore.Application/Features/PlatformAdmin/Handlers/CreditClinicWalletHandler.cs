using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Internal admin crediting a clinic's wallet directly — a goodwill credit or a compensation. No
/// payment is involved, so this is the one place money enters a wallet the gateway never confirmed;
/// that is exactly why it is idempotent on the admin's reference and always attributed to an admin in
/// the ledger.
/// </summary>
public sealed class CreditClinicWalletHandler(
    IClinicWalletRepository walletRepository,
    IClinicWalletTransactionRepository walletTransactionRepository,
    IOptions<UsageBillingOptions> options,
    ILogger<CreditClinicWalletHandler> logger)
    : IRequestHandler<CreditClinicWalletCommand, decimal>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task<decimal> Handle(CreditClinicWalletCommand command, CancellationToken ct)
    {
        // A replay is not an error: the first call already did the work and the caller is asking what
        // the balance is. Answering with the current balance is safe; crediting again is not.
        if (await walletTransactionRepository.ExistsAsync(
                command.ApplicationId, WalletReferenceType.AdminCredit, command.ReferenceId, ct))
        {
            var current = await walletRepository.GetByApplicationIdNoTrackingAsync(command.ApplicationId, ct);

            logger.LogInformation(
                "Admin credit {ReferenceId} for clinic {ApplicationId} was already applied — no-op.",
                command.ReferenceId, command.ApplicationId);

            return current?.Balance ?? 0m;
        }

        var utcNow = DateTime.UtcNow;

        // Tracked get-or-create: the clinic may never have been funded before, and the balance plus
        // its ledger row have to move in the same SaveChanges.
        var wallet = await walletRepository.GetByApplicationIdAsync(command.ApplicationId, ct);
        if (wallet is null)
        {
            wallet = ClinicWallet.Create(
                command.ApplicationId, _options.DefaultLowBalanceThreshold, _options.Currency);
            await walletRepository.AddAsync(wallet, ct);
        }

        wallet.Credit(command.Amount, utcNow);

        var ledgerRow = ClinicWalletTransaction.CreateAdminCredit(
            command.ApplicationId, command.Amount, wallet.Balance, command.ReferenceId,
            "Admin credit", utcNow, command.AdminEmail);

        await walletTransactionRepository.AddAsync(ledgerRow, ct);
        await walletRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminEmail} credited {Amount} {Currency} to clinic {ApplicationId}; new balance {Balance}.",
            command.AdminEmail, command.Amount, wallet.Currency, command.ApplicationId, wallet.Balance);

        return wallet.Balance;
    }
}
