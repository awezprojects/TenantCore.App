using MediatR;

namespace TenantCore.Application.Features.Tenants.Commands;

public record DeleteTenantCommand(Guid Id) : IRequest;
