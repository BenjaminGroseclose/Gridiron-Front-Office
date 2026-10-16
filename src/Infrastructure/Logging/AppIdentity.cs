using System.Reflection;

namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>
/// Which build is running, and which run this is. The version and commit come from the entry assembly's informational
/// version, which the .NET SDK stamps as <c>0.0.1+&lt;commit sha&gt;</c> when building from git.
/// </summary>
public sealed record AppIdentity(string Version, string? Commit, string Configuration, Guid SessionId)
{
    /// <summary>"0.0.1+ce3e6f6", or just the version when no commit was stamped.</summary>
    public string Display => Commit is null ? Version : $"{Version}+{Commit}";

    public static AppIdentity From(Assembly entry, string configuration)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var informational = entry.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? entry.GetName().Version?.ToString(3)
            ?? "0.0.0";
        return Parse(informational, configuration);
    }

    /// <summary>Splits an informational version like <c>0.0.1+ce3e6f6a…</c> into the version and a 7-character commit.</summary>
    public static AppIdentity Parse(string informational, string configuration)
    {
        ArgumentNullException.ThrowIfNull(informational);
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        var version = plus < 0 ? informational : informational[..plus];
        var commit = plus < 0 ? null : informational[(plus + 1)..];
        if (commit is { Length: > 7 })
        {
            commit = commit[..7];
        }

        return new AppIdentity(version, string.IsNullOrEmpty(commit) ? null : commit, configuration, Guid.NewGuid());
    }
}
