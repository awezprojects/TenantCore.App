using FluentAssertions;
using TenantCore.Domain.Common;
using TenantCore.Domain.Tests.TestData;

namespace TenantCore.Domain.Tests.Common;

public class AuditableEntityTests
{
    [Fact]
    public void SetCreatedBy_WhenCalled_SetsCreatedBy()
    {
        var entity = new TestAuditableEntity();

        entity.SetCreatedBy(DomainTestData.CreatedBy);

        entity.CreatedBy.Should().Be(DomainTestData.CreatedBy);
    }

    [Fact]
    public void SetUpdatedBy_WhenCalled_SetsUpdatedByAndUpdatedAt()
    {
        var entity = new TestAuditableEntity();

        entity.SetUpdatedBy(DomainTestData.UpdatedBy);

        entity.UpdatedBy.Should().Be(DomainTestData.UpdatedBy);
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    private sealed class TestAuditableEntity : AuditableEntity;
}
