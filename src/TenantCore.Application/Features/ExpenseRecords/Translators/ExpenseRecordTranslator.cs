using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Translators;

public static class ExpenseRecordTranslator
{
    public static ExpenseRecord ToEntity(CreateExpenseRecordCommand command, string categoryName)
        => ExpenseRecord.Create(
            command.ApplicationId,
            command.Request.ExpenseCategoryId,
            categoryName,
            command.Request.Amount,
            command.Request.Notes,
            command.RecordedByUserId,
            command.Request.CounterSessionId);

    public static ExpenseRecordDto ToDto(ExpenseRecord entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        ExpenseCategoryId = entity.ExpenseCategoryId,
        CategoryName = entity.CategoryName,
        Amount = entity.Amount,
        PaidAmount = entity.PaidAmount,
        Notes = entity.Notes,
        RecordedByUserId = entity.RecordedByUserId,
        RecordedAt = DateTime.SpecifyKind(entity.RecordedAt, DateTimeKind.Utc),
        CounterSessionId = entity.CounterSessionId
    };

    public static ExpenseRecordSummaryDto ToSummaryDto(ExpenseRecord entity) => new()
    {
        Id = entity.Id,
        CategoryName = entity.CategoryName,
        Amount = entity.Amount,
        PaidAmount = entity.PaidAmount,
        Notes = entity.Notes,
        RecordedAt = DateTime.SpecifyKind(entity.RecordedAt, DateTimeKind.Utc)
    };
}
