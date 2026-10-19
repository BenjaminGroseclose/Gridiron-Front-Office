namespace GridironFrontOffice.UI.Shell;

/// <summary>How the host frames the app.</summary>
/// <param name="AppBarIsTitleBar">
/// The app bar is the window's title bar (the desktop host, D-068): it's a drag region and keeps 138 px free on the
/// right for the WPF caption buttons. False in the DevHost, where the browser has its own frame.
/// </param>
public sealed record ShellOptions(bool AppBarIsTitleBar = false);
