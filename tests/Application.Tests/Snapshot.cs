using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GridironFrontOffice.Application.Tests;

/// <summary>
/// Hand-rolled snapshot tests (D-085): a value as tab-indented JSON, compared with <c>{name}.verified.json</c> in this
/// project's source folder, line endings ignored. On a mismatch, or with no approved file yet, it writes
/// <c>{name}.received.json</c> beside it and fails; renaming that over the verified file approves the change.
/// </summary>
internal static class Snapshot
{
	// Enums by name; readable names ("Brädy", "O'Neil") rather than \u escapes
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		IndentCharacter = '\t',
		IndentSize = 1,
		NewLine = "\n",
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		Converters = { new JsonStringEnumConverter() },
	};

	private static readonly DirectoryInfo Project = FindProject();

	/// <param name="value">What to snapshot: deterministic values only, since there are no scrubbers.</param>
	/// <param name="name">The path under <c>tests/Application.Tests</c>, without the extension: <c>Leagues/GoldenLeagueTests.Seed42</c>.</param>
	public static void Match(object value, string name)
	{
		var received = JsonSerializer.Serialize(value, Options) + "\n";
		var path = Path.Combine(Project.FullName, name.Replace('/', Path.DirectorySeparatorChar));
		var verifiedPath = path + ".verified.json";
		var receivedPath = path + ".received.json";
		var verified = File.Exists(verifiedPath) ? File.ReadAllText(verifiedPath).ReplaceLineEndings("\n") : null;

		if (received == verified)
		{
			// A stale one from an earlier failure
			File.Delete(receivedPath);
			return;
		}

		File.WriteAllText(receivedPath, received);

		if (verified is null)
		{
			Assert.Fail($"No approved snapshot at {verifiedPath}. Read {receivedPath}, then rename it to approve it.");
		}

		var (line, was, now) = FirstDifference(verified, received);
		Assert.Fail($"The snapshot differs from {verifiedPath} at line {line}:\n  approved: {was}\n  received: {now}\nRead the whole diff against {receivedPath}; rename it over the approved file only for a change you meant.");
	}

	private static (int Line, string Was, string Now) FirstDifference(string verified, string received)
	{
		var (was, now) = (verified.Split('\n'), received.Split('\n'));
		var i = 0;

		while (i < was.Length && i < now.Length && was[i] == now[i])
		{
			i++;
		}

		return (i + 1, i < was.Length ? was[i].Trim() : "(the end)", i < now.Length ? now[i].Trim() : "(the end)");
	}

	private static DirectoryInfo FindProject()
	{
		for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			if (File.Exists(Path.Combine(dir.FullName, "GridironFrontOffice.slnx")))
			{
				return new DirectoryInfo(Path.Combine(dir.FullName, "tests", "Application.Tests"));
			}
		}

		throw new InvalidOperationException($"GridironFrontOffice.slnx not found above {AppContext.BaseDirectory}.");
	}
}
