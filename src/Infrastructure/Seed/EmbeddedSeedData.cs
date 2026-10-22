using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Seed;

/// <summary>
/// <see cref="ISeedData"/> from the files embedded in this assembly (WP2's league data, WP3's player data). Each file
/// loads once, the first time it's asked for, and logs how long it took (events 1200–1202).
/// </summary>
internal sealed class EmbeddedSeedData(ILogger<EmbeddedSeedData> log) : ISeedData
{
	private readonly Lazy<TeamSet> _teamSet = new(() => Load<TeamSet>(log, "league/teams.json"));
	private readonly Lazy<CollegeList> _collegeList = new(() => Load<CollegeList>(log, "league/colleges.json"));
	private readonly Lazy<PositionData> _positions = new(() => Load<PositionData>(log, "players/positions.json"));
	private readonly Lazy<GenerationSettings> _generation = new(() => Load<GenerationSettings>(log, "players/generation.json"));
	private readonly Lazy<JerseyRules> _jerseyRules = new(() => Load<JerseyRules>(log, "players/jersey-rules.json"));
	private readonly Lazy<NameList> _firstNames = new(() => Load<NameList>(log, "names/first.json"));
	private readonly Lazy<NameList> _lastNames = new(() => Load<NameList>(log, "names/last.json"));
	private readonly Lazy<HometownList> _hometowns = new(() => Load<HometownList>(log, "places/hometowns.json"));
	private readonly Lazy<IReadOnlyList<string>> _famousPeople = new(() => Load<List<string>>(log, "blocklists/famous-people.json"));

	public TeamSet TeamSet => _teamSet.Value;
	public CollegeList Colleges => _collegeList.Value;
	public PositionData Positions => _positions.Value;
	public GenerationSettings Generation => _generation.Value;
	public JerseyRules JerseyRules => _jerseyRules.Value;
	public NameList FirstNames => _firstNames.Value;
	public NameList LastNames => _lastNames.Value;
	public HometownList Hometowns => _hometowns.Value;
	public IReadOnlyList<string> FamousPeople => _famousPeople.Value;

	/// <summary>
	/// Strict parsing: a missing field, a null where text belongs, an unknown field ("stripes") or a number for an
	/// enum is an error. A field added later has a default value, so older files still load (D-077).
	/// </summary>
	internal static JsonSerializerOptions JsonOptions { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		RespectNullableAnnotations = true,
		RespectRequiredConstructorParameters = true,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
		Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
	};

	/// <summary>Reads one of the WP3 files and logs it under its resource name (event 1202).</summary>
	private static T Load<T>(ILogger<EmbeddedSeedData> log, string path) where T : class
	{
		var started = Stopwatch.GetTimestamp();
		var value = Read<T>(path);
		var elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
		SeedLog.ResourceLoaded(log, path, elapsedMs);
		return value;
	}

	private static T Read<T>(string path) where T : class
	{
		using var stream = typeof(EmbeddedSeedData).Assembly.GetManifestResourceStream(path)
			?? throw new InvalidOperationException($"Embedded resource {path} not found.");
		return JsonSerializer.Deserialize<T>(stream, JsonOptions)
			?? throw new InvalidOperationException($"Embedded resource {path} deserialized to null.");
	}
}
