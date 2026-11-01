namespace TenantCore.Application.Common;

/// <summary>
/// Bound from the "DummyData" configuration section. Defaults to false, and it is checked
/// independently of TenantCore.Admin's own copy: the portal hides the Test Data section, the App
/// refuses every dummy-data route with 403, and neither trusts the other's check. The endpoint set
/// therefore does not exist in Production.
/// </summary>
public sealed class DummyDataOptions
{
    public const string SectionName = "DummyData";

    public bool Enabled { get; set; }
}
