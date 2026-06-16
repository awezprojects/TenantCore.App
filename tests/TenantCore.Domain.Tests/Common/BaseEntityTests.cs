using FluentAssertions;
using TenantCore.Domain.Common;

namespace TenantCore.Domain.Tests.Common;

public class BaseEntityTests
{
    [Fact]
    public void Constructor_WhenCreated_InitializesDefaultProperties()
    {
        var entity = new TestBaseEntity();

        entity.Id.Should().NotBeEmpty();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
        entity.RowVersion.Should().BeNull();
    }

    [Fact]
    public void SetUpdatedAt_WhenCalled_SetsUpdatedAtToCurrentUtcTime()
    {
        var entity = new TestBaseEntity();

        entity.InvokeSetUpdatedAt();

        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    private sealed class TestBaseEntity : BaseEntity
    {
        public void InvokeSetUpdatedAt() => SetUpdatedAt();
    }
}
