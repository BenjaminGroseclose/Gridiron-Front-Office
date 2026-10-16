namespace GridironFrontOffice.Desktop.Spike;

/// <summary>Spike S0c: the three title bar variants under comparison (M0_FOUNDATIONS.md WP4).</summary>
internal enum TitleBarMode
{
    /// <summary>The standard Windows title bar, recolored through DWM. The baseline and the fallback.</summary>
    Native,

    /// <summary>Option (a): WindowChrome with a slim WPF caption strip above the web content.</summary>
    Chrome,

    /// <summary>Option (b): the HTML top bar is the title bar; WPF caption buttons sit over its right end.</summary>
    Html,
}

/// <summary>
/// Spike S0c command-line switches: <c>--titlebar=native|chrome|html</c>, <c>--light</c>, <c>--no-fallback</c>
/// (turns off the Ground-colored window, WebView2 and page backgrounds, to show what a cold start looks like without them),
/// <c>--no-boot-hide</c> (shows the page before the theme lands) and <c>--os-light</c> (WebView2 reports a light Windows).
/// </summary>
internal sealed record SpikeOptions(TitleBarMode TitleBar, bool Dark, bool Fallback, bool BootHide, bool OsLight)
{
    public static SpikeOptions Parse(IEnumerable<string> args)
    {
        var mode = TitleBarMode.Chrome;
        var dark = true;
        var fallback = true;
        var bootHide = true;
        var osLight = false;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--titlebar=", StringComparison.OrdinalIgnoreCase))
            {
                mode = Enum.Parse<TitleBarMode>(arg["--titlebar=".Length..], ignoreCase: true);
            }
            else if (arg.Equals("--light", StringComparison.OrdinalIgnoreCase))
            {
                dark = false;
            }
            else if (arg.Equals("--no-fallback", StringComparison.OrdinalIgnoreCase))
            {
                fallback = false;
            }
            else if (arg.Equals("--no-boot-hide", StringComparison.OrdinalIgnoreCase))
            {
                bootHide = false;
            }
            else if (arg.Equals("--os-light", StringComparison.OrdinalIgnoreCase))
            {
                osLight = true;
            }
        }

        return new SpikeOptions(mode, dark, fallback, bootHide && fallback, osLight);
    }
}
