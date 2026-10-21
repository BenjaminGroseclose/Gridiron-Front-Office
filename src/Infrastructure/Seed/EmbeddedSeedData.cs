using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using GridironFrontOffice.Application.Leagues;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Seed;

internal sealed class EmbeddedSeedData(ILogger<EmbeddedSeedData> log) : ISeedData
{
    private readonly Lazy<TeamSet> _teamSet = new(() => LoadTeamSet(log));
    private readonly Lazy<CollegeList> _collegeList = new(() => LoadCollegeList(log));

    public TeamSet TeamSet => _teamSet.Value;
    public CollegeList Colleges => _collegeList.Value;

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

    private static TeamSet LoadTeamSet(ILogger<EmbeddedSeedData> log)
    {
        var started = Stopwatch.GetTimestamp();
        var teamSet = Read<TeamSet>("league/teams.json");
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        SeedLog.TeamSetLoaded(log, teamSet.Name, teamSet.Teams.Count, (long)elapsedMs);
        return teamSet;
    }

    private static CollegeList LoadCollegeList(ILogger<EmbeddedSeedData> log)
    {
        var started = Stopwatch.GetTimestamp();
        var collegeList = Read<CollegeList>("league/colleges.json");
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        SeedLog.CollegeListLoaded(log, collegeList.Name, collegeList.Colleges.Count, (long)elapsedMs);
        return collegeList;
    }


    private static T Read<T>(string path) where T : class
    {
        using var stream = typeof(EmbeddedSeedData).Assembly.GetManifestResourceStream(path)
            ?? throw new InvalidOperationException($"Embedded resource {path} not found.");
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Embedded resource {path} deserialized to null.");
    }
}
