using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.MedicineDosageForms.Commands;

public sealed record DeleteMedicineDosageFormCommand(Guid Id) : IRequest, IBusinessAction
{
    public string ActionName => "Medicine Dosage Form Deletion";
}
