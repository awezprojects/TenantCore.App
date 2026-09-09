using FluentAssertions;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Tests.Services;

public class TriStateCheckboxStateTests
{
    [Fact]
    public void Compute_AllTrue_ReturnsChecked()
    {
        TriStateCheckboxState.Compute([true, true, true]).Should().Be(TriStateCheckboxValue.Checked);
    }

    [Fact]
    public void Compute_AllFalse_ReturnsUnchecked()
    {
        TriStateCheckboxState.Compute([false, false]).Should().Be(TriStateCheckboxValue.Unchecked);
    }

    [Fact]
    public void Compute_Mixed_ReturnsIndeterminate()
    {
        TriStateCheckboxState.Compute([true, false, true]).Should().Be(TriStateCheckboxValue.Indeterminate);
    }

    [Fact]
    public void Compute_EmptyList_ReturnsUnchecked()
    {
        TriStateCheckboxState.Compute([]).Should().Be(TriStateCheckboxValue.Unchecked);
    }

    [Fact]
    public void Compute_SingleTrue_ReturnsChecked()
    {
        TriStateCheckboxState.Compute([true]).Should().Be(TriStateCheckboxValue.Checked);
    }

    [Fact]
    public void Compute_SingleFalse_ReturnsUnchecked()
    {
        TriStateCheckboxState.Compute([false]).Should().Be(TriStateCheckboxValue.Unchecked);
    }
}
