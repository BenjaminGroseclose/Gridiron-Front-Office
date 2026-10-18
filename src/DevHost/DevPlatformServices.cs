using System.Diagnostics;
using System.Globalization;
using GridironFrontOffice.Application.Platform;

namespace GridironFrontOffice.DevHost;

/// <summary>
/// The DevHost's <see cref="IPlatformServices"/>: "deleted" files go to a <c>.trash</c> folder in the dev data folder
/// (one subfolder per delete), so development never fills the real Recycle Bin. Show in folder still opens Explorer,
/// since the DevHost runs on the developer's own machine.
/// </summary>
internal sealed class DevPlatformServices(IAppPaths appPaths, TimeProvider time) : IPlatformServices
{
    public bool SendToRecycleBin(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var stamp = time.GetLocalNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var bin = Path.Combine(appPaths.Root, ".trash", stamp);
        Directory.CreateDirectory(bin);
        foreach (var path in paths)
        {
            var target = Path.Combine(bin, Path.GetFileName(path));
            if (Directory.Exists(path))
            {
                Directory.Move(path, target);
            }
            else
            {
                File.Move(path, target);
            }
        }

        return true;
    }

    public void ShowInFolder(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe") { Arguments = $"/select,\"{path}\"", UseShellExecute = false })?.Dispose();
}
