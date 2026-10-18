using GridironFrontOffice.Application.LiveEvents;
using GridironFrontOffice.Application.Platform;

namespace GridironFrontOffice.Infrastructure.Tests.Saves;

/// <summary>
/// The tests' Recycle Bin: records each call and moves the items into a <c>.trash</c> folder, so the real Recycle Bin
/// is never touched by a build. <see cref="PlayerAgrees"/> = false plays the player saying no to a permanent delete.
/// </summary>
internal sealed class FakePlatformServices(string trash) : IPlatformServices
{
    public List<IReadOnlyList<string>> Recycled { get; } = [];

    public bool PlayerAgrees { get; set; } = true;

    public string Trash { get; } = trash;

    public bool SendToRecycleBin(IReadOnlyList<string> paths)
    {
        if (!PlayerAgrees)
        {
            return false;
        }

        Recycled.Add(paths);
        var bin = Path.Combine(Trash, Recycled.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
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

    public void ShowInFolder(string path) => throw new NotSupportedException("Tests never open Explorer.");
}

/// <summary>A live event that's running (or not) when a test says so.</summary>
internal sealed class FakeLiveEvents : ILiveEventStatus
{
    public bool IsRunning { get; set; }
}
