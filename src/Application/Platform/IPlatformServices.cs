namespace GridironFrontOffice.Application.Platform;

/// <summary>
/// What GFO needs from the operating system (ARCHITECTURE.md §4.1, D-003). The desktop host implements it with the
/// Windows shell; the DevHost with a <c>.trash</c> folder, so development never fills the real Recycle Bin.
/// </summary>
public interface IPlatformServices
{
    /// <summary>
    /// Moves files and folders to the Recycle Bin. Nothing is deleted permanently without the player agreeing: when
    /// Windows can't recycle something (too big, or a drive without a Recycle Bin), it asks first. Returns false when
    /// the player said no. Throws <see cref="IOException"/> when something couldn't be moved.
    /// </summary>
    bool SendToRecycleBin(IReadOnlyList<string> paths);

    /// <summary>Opens the containing folder with the file selected (the "Show in folder" button).</summary>
    void ShowInFolder(string path);

    /// <summary>
    /// Quits the app the way its window's close button does (the main menu's Exit): the open league is closed and the
    /// window placement saved first. Returns at once; the close finishes on the host's own thread. The DevHost stops
    /// its server instead.
    /// </summary>
    void Quit();
}
