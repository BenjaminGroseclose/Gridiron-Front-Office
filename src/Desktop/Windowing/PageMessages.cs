using System.Text.Json;

namespace GridironFrontOffice.Desktop.Windowing;

/// <summary>A rectangle in CSS pixels, relative to the web view (CSS px equal DIPs: page zoom is off).</summary>
internal readonly record struct CssRect(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y) => x >= X && x < X + Width && y >= Y && y < Y + Height;
}

/// <summary>A drag region (an element with <c>data-drag-region</c>) and the interactive holes inside it.</summary>
internal sealed record DragRegion(CssRect Bar, IReadOnlyList<CssRect> Holes)
{
    /// <summary>True over the bar's empty space: inside the bar and outside every hole.</summary>
    public bool IsCaption(double x, double y) => Bar.Contains(x, y) && !Holes.Any(h => h.Contains(x, y));
}

/// <summary>What gfo-host.js tells the window.</summary>
internal abstract record PageMessage;

/// <summary>The page's drag regions changed (layout, navigation, fonts).</summary>
internal sealed record DragRegionsMessage(IReadOnlyList<DragRegion> Regions) : PageMessage;

/// <summary>The theme service applied a mode (the <c>data-theme</c> attribute on the root element).</summary>
internal sealed record ThemeModeMessage(bool Dark) : PageMessage;

/// <summary>
/// Reads the messages gfo-host.js posts through WebView2 (<c>chrome.webview.postMessage</c>). They're JSON strings with
/// a <c>gfo</c> kind. BlazorWebView shares the channel and ignores strings without its own <c>__bwv:</c> prefix.
/// </summary>
internal static class PageMessages
{
    /// <summary>The message, or null when it isn't one of ours or is malformed.</summary>
    public static PageMessage? Parse(string? json)
    {
        if (string.IsNullOrEmpty(json) || json[0] != '{')
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("gfo", out var kind))
            {
                return null;
            }

            return kind.GetString() switch
            {
                "drag" => new DragRegionsMessage([.. root.GetProperty("regions").EnumerateArray().Select(ReadRegion)]),
                "theme" => new ThemeModeMessage(root.GetProperty("mode").GetString() == "dark"),
                _ => null,
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or IndexOutOfRangeException or FormatException)
        {
            return null;
        }
    }

    private static DragRegion ReadRegion(JsonElement region) =>
        new(ReadRect(region.GetProperty("bar")), [.. region.GetProperty("holes").EnumerateArray().Select(ReadRect)]);

    /// <summary>A rectangle is <c>[x, y, width, height]</c>.</summary>
    private static CssRect ReadRect(JsonElement rect) =>
        new(rect[0].GetDouble(), rect[1].GetDouble(), rect[2].GetDouble(), rect[3].GetDouble());
}
