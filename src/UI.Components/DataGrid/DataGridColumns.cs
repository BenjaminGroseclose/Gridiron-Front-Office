using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.DataGrid;

/// <summary>
/// A column showing one property, sorted by it: Fluent's <c>PropertyColumn</c> with GFO's rules (UI_DESIGN_SYSTEM.md
/// §5.3). MinWidth = Width (Fluent defaults to 100 px), the compact header, and numbers right-aligned in tabular figures,
/// sorting high to low first. It's a subclass rather than a wrapper because the grid only collects columns rendered
/// directly in its content.
/// </summary>
public class DataGridColumn<TItem, TValue> : PropertyColumn<TItem, TValue>
{
    [CascadingParameter] public DataGrid<TItem>? Owner { get; set; }

    /// <summary>Right-aligned tabular figures, and the first sort click goes high to low.</summary>
    [Parameter] public bool Numeric { get; set; }

    /// <summary>Pins the column to the start (the player's name). Pin only the first column.</summary>
    [Parameter] public bool Pinned { get; set; }

    protected override void OnParametersSet()
    {
        Sortable ??= true;
        MinWidth = DataGridColumnRules.RequireWidth(Title, Width);
        Align = Numeric ? DataGridCellAlignment.End : DataGridCellAlignment.Start;
        if (Numeric)
        {
            Class = string.IsNullOrEmpty(Class) ? "gfo-num" : $"{Class} gfo-num";
        }

        Pin = Pinned ? DataGridColumnPin.Start : DataGridColumnPin.None;
        HeaderCellItemTemplate = Owner?.HeaderFor(Sortable.Value, Numeric);
        base.OnParametersSet();
    }
}

/// <summary>
/// A column with its own cell markup (a rating chip, money, a sparkline): Fluent's <c>TemplateColumn</c> with GFO's
/// rules. Sortable when it has a <c>SortBy</c>, which is always defined ascending: Fluent reverses it for descending (S0b).
/// </summary>
public class DataGridTemplateColumn<TItem> : TemplateColumn<TItem>
{
    [CascadingParameter] public DataGrid<TItem>? Owner { get; set; }

    /// <summary>Right-aligned tabular figures, and the first sort click goes high to low.</summary>
    [Parameter] public bool Numeric { get; set; }

    [Parameter] public bool Pinned { get; set; }

    protected override void OnParametersSet()
    {
        Sortable = SortBy is not null;
        MinWidth = DataGridColumnRules.RequireWidth(Title, Width);
        Align = Numeric ? DataGridCellAlignment.End : DataGridCellAlignment.Start;
        if (Numeric)
        {
            Class = string.IsNullOrEmpty(Class) ? "gfo-num" : $"{Class} gfo-num";
        }

        Pin = Pinned ? DataGridColumnPin.Start : DataGridColumnPin.None;
        HeaderCellItemTemplate = Owner?.HeaderFor(Sortable.Value, Numeric);
        base.OnParametersSet();
    }
}

internal static class DataGridColumnRules
{
    /// <summary>Every GFO column has a fixed width, which is also its minimum (S0b). Returns it.</summary>
    public static string RequireWidth(string? title, string? width) =>
        string.IsNullOrWhiteSpace(width)
            ? throw new InvalidOperationException($"DataGrid column '{title}' needs a Width, such as 72px.")
            : width;
}
