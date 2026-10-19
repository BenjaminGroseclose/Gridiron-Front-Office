using System.Collections.Concurrent;
using FluentIconBase = Microsoft.FluentUI.AspNetCore.Components.Icon;

namespace GridironFrontOffice.UI.Components.Icons;

/// <summary>
/// The icons GFO uses, by GFO name (VISUAL_DESIGN.md §7). Each name is the Fluent System Icons name today; swapping the
/// set means changing <see cref="IconCatalog"/>, not the screens. Add a name here when a screen needs one: a test checks
/// that every name exists in every size and in both styles.
/// </summary>
public enum IconName
{
    Add,
    Alert,
    ArrowLeft,
    ArrowSortDown,
    ArrowSortUp,
    Board,
    Calendar,
    CheckmarkCircle,
    ChevronDown,
    ChevronLeft,
    ChevronRight,
    ChevronUp,
    DataBarVertical,
    Delete,
    Desktop,
    Dismiss,
    Document,
    DocumentError,
    ErrorCircle,
    Filter,
    Folder,
    FolderOpen,
    Home,
    Info,
    Keyboard,
    MoreHorizontal,
    Navigation,
    Open,
    PaintBrush,
    People,
    PersonCircle,
    Pin,
    Play,
    Save,
    Search,
    Settings,
    Table,
    TextDensity,
    Trophy,
    Warning,
    WeatherMoon,
    WeatherSunny,
}

/// <summary>Resolves a GFO icon name to a Fluent System Icons instance (Regular, or Filled for active states).</summary>
public static class IconCatalog
{
    /// <summary>The sizes GFO uses: 16 inline, 20 for buttons (and 18 px nav icons drawn from it), 24 for empty states.</summary>
    public static IReadOnlyList<int> Sizes { get; } = [16, 20, 24];

    private static readonly ConcurrentDictionary<(IconName Name, int Size, bool Filled), Type> Types = new();

    public static FluentIconBase Create(IconName name, int size = 20, bool filled = false)
    {
        if (!Sizes.Contains(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "GFO icons come in 16, 20 and 24 px.");
        }

        var type = Types.GetOrAdd((name, size, filled), static key =>
        {
            var style = key.Filled ? "Filled" : "Regular";
            var typeName = $"Microsoft.FluentUI.AspNetCore.Components.Icons.{style}.Size{key.Size}+{key.Name}, Microsoft.FluentUI.AspNetCore.Components.Icons.{style}";
            return Type.GetType(typeName, throwOnError: true)!;
        });

        return (FluentIconBase)Activator.CreateInstance(type)!;
    }
}
