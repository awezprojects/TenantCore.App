using TenantCore.Application.Features.ExpenseCategories.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Translators;

public static class ExpenseCategoryTranslator
{
    public static ExpenseCategory ToEntity(CreateExpenseCategoryCommand command)
        => ExpenseCategory.Create(command.ApplicationId, command.Request.Name, command.Request.Description);

    public static ExpenseCategoryDto ToDto(ExpenseCategory entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive
    };

    public static ExpenseCategorySummaryDto ToSummaryDto(ExpenseCategory entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive
    };
}
