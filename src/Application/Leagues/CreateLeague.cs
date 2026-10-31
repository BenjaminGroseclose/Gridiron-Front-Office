using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Leagues;

/// <summary>What the wizard asks for (F-01 v0): a name, the seed (random unless typed) and the season (D-073: this year).</summary>
public sealed record NewLeague(string Name, ulong Seed, int Season);

/// <summary>What CreateLeague is doing, for the wizard's progress.</summary>
public enum LeagueCreationStep { Generating, Saving }

/// <summary>
/// New League (WP5): generates the league outside any transaction, then creates the save and the league in one command
/// (D-089). The same seed and season always make the same league. The user's team is picked afterwards, in its own
/// command (LeagueCommands).
/// </summary>
public sealed class CreateLeague(ISaveManager saves, LeagueGenerator generator, ISeedData data, TimeProvider time,
	ILogger<CreateLeague> log)
{
	public async Task<Result<OpenSave>> RunAsync(NewLeague league, IProgress<LeagueCreationStep>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(league);
		var started = time.GetTimestamp();

		progress?.Report(LeagueCreationStep.Generating);
		// CPU only, so off the caller's thread: the wizard keeps painting while it runs
		var generated = await Task.Run(() => generator.Generate(league.Seed, league.Season), cancellationToken)
			.ConfigureAwait(false);

		progress?.Report(LeagueCreationStep.Saving);
		var created = await saves.CreateAsync(league.Name,
			(uow, ct) => LeagueWriter.WriteAsync(uow, generated, data, ct), cancellationToken).ConfigureAwait(false);

		if (created.IsSuccess)
		{
			var everyone = generated.Teams.SelectMany(t => t.Players).Concat(generated.FreeAgents).ToList();
			var contracts = everyone.Count(p => p.Contract is not null);
			var elapsedMs = (long)time.GetElapsedTime(started).TotalMilliseconds;
			LeagueLog.LeagueCreated(log, created.Value.LeagueName, generated.Teams.Count, everyone.Count, contracts, league.Seed,
				league.Season, elapsedMs);
		}

		return created;
	}
}
