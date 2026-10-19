using GridironFrontOffice.UI.Components.Icons;

namespace GridironFrontOffice.UI.Components;

/// <summary>One choice in a <see cref="SegmentedControl{TValue}"/>.</summary>
public sealed record SegmentedOption<TValue>(TValue Value, string Label, IconName? Icon = null);
