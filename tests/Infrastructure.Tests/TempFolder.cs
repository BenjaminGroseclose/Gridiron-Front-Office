namespace GridironFrontOffice.Infrastructure.Tests;

/// <summary>A fresh folder under %TEMP% per test, removed afterwards.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gfo-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A file still held by a just-disposed logger is cleaned up with %TEMP% later.
        }
    }
}
