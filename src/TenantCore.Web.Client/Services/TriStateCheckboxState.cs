namespace TenantCore.Web.Client.Services;

public enum TriStateCheckboxValue
{
    Unchecked,
    Checked,
    Indeterminate
}

/// <summary>
/// Pure logic for a "select all" checkbox driven by a list of individual flags —
/// used by the print-on-prescription toggle in HistoryChipField.
/// </summary>
public static class TriStateCheckboxState
{
    public static TriStateCheckboxValue Compute(IReadOnlyList<bool> flags)
    {
        if (flags.Count == 0) return TriStateCheckboxValue.Unchecked;
        if (flags.All(f => f)) return TriStateCheckboxValue.Checked;
        if (flags.All(f => !f)) return TriStateCheckboxValue.Unchecked;
        return TriStateCheckboxValue.Indeterminate;
    }
}
