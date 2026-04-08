using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Domain.Tests.Entities;

public class TenantTests
{
    [Fact] public void Create_ValidInputs_CreatesSuccessfully() { var t = Tenant.Create("Test Corp","test-corp","A test"); t.Name.Should().Be("Test Corp"); t.Subdomain.Should().Be("test-corp"); t.IsActive.Should().BeTrue(); }
    [Fact] public void Create_EmptyName_ThrowsDomainValidationException() { var act = () => Tenant.Create("","test-corp"); act.Should().Throw<DomainValidationException>(); }
    [Fact] public void Create_EmptySubdomain_ThrowsDomainValidationException() { var act = () => Tenant.Create("Test Corp",""); act.Should().Throw<DomainValidationException>(); }
    [Fact] public void Create_SubdomainNormalized_ToLowerCase() { var t = Tenant.Create("Test","MyTenant"); t.Subdomain.Should().Be("mytenant"); }
    [Fact] public void Deactivate_ActiveTenant_BecomesInactive() { var t = Tenant.Create("Test","test"); t.Deactivate(); t.IsActive.Should().BeFalse(); }
    [Fact] public void Activate_InactiveTenant_BecomesActive() { var t = Tenant.Create("Test","test"); t.Deactivate(); t.Activate(); t.IsActive.Should().BeTrue(); }
    [Fact] public void Update_ValidInputs_UpdatesSuccessfully() { var t = Tenant.Create("Old","old"); t.Update("New","new",null,null,null,null); t.Name.Should().Be("New"); t.UpdatedAt.Should().NotBeNull(); }
}
