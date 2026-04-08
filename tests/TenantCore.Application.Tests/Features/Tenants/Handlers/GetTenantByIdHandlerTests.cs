using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.Tenants.Handlers;
using TenantCore.Application.Features.Tenants.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Tenants.Handlers;

public class GetTenantByIdHandlerTests
{
    private readonly Mock<ITenantRepository> _repoMock = new();
    private readonly Mock<ILogger<GetTenantByIdHandler>> _loggerMock = new();

    [Fact] public async Task Handle_ExistingTenant_ReturnsDto() { var t = Tenant.Create("Acme","acme","Test"); _repoMock.Setup(r=>r.GetByIdAsync(t.Id,It.IsAny<CancellationToken>())).ReturnsAsync(t); var h = new GetTenantByIdHandler(_repoMock.Object,_loggerMock.Object); var r = await h.Handle(new GetTenantByIdQuery(t.Id),CancellationToken.None); r.Should().NotBeNull(); r!.Name.Should().Be("Acme"); }
    [Fact] public async Task Handle_NonExistentTenant_ReturnsNull() { _repoMock.Setup(r=>r.GetByIdAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Tenant?)null); var h = new GetTenantByIdHandler(_repoMock.Object,_loggerMock.Object); var r = await h.Handle(new GetTenantByIdQuery(Guid.NewGuid()),CancellationToken.None); r.Should().BeNull(); }
}
