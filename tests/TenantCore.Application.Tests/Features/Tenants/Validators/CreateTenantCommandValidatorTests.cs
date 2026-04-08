using FluentAssertions;
using TenantCore.Application.Features.Tenants.Commands;
using TenantCore.Application.Features.Tenants.Validators;

namespace TenantCore.Application.Tests.Features.Tenants.Validators;

public class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();
    [Fact] public async Task Validate_ValidCommand_NoErrors() { var r = await _validator.ValidateAsync(new CreateTenantCommand("Test","test",null,null,null,null)); r.IsValid.Should().BeTrue(); }
    [Fact] public async Task Validate_EmptyName_HasError() { var r = await _validator.ValidateAsync(new CreateTenantCommand("","test",null,null,null,null)); r.IsValid.Should().BeFalse(); r.Errors.Should().Contain(e=>e.PropertyName=="Name"); }
    [Fact] public async Task Validate_InvalidSubdomainWithUppercase_HasError() { var r = await _validator.ValidateAsync(new CreateTenantCommand("Test","MyTenant",null,null,null,null)); r.IsValid.Should().BeFalse(); }
    [Fact] public async Task Validate_InvalidEmail_HasError() { var r = await _validator.ValidateAsync(new CreateTenantCommand("Test","test",null,"not-email",null,null)); r.IsValid.Should().BeFalse(); }
    [Fact] public async Task Validate_ValidEmail_NoError() { var r = await _validator.ValidateAsync(new CreateTenantCommand("Test","test",null,"a@b.com",null,null)); r.IsValid.Should().BeTrue(); }
}
