using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.ExpenseCategories.Commands;
using TenantCore.Application.Features.ExpenseCategories.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/expense-categories")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ExpenseCategoriesController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetExpenseCategoriesQuery(GetApplicationId()), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetExpenseCategoryByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateExpenseCategoryCommand(request, GetApplicationId()), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseCategoryRequest request, CancellationToken ct)
        => Ok(await sender.Send(new UpdateExpenseCategoryCommand(id, request, GetApplicationId()), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteExpenseCategoryCommand(id, GetApplicationId()), ct);
        return NoContent();
    }
}
