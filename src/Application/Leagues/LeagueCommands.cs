using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Leagues;

/// <summary>Raised when the user's team is picked; WP6's GameStateStore reloads on it.</summary>
public sealed record UserTeamPicked(TeamID Team) : IDomainEvent;

/// <summary>The league's commands. M1 has one: the user's team, stored only in SaveInfo (D-078 #7).</summary>
public sealed class LeagueCommands(ISaveWriter writer, ILogger<LeagueCommands> log)
{
	/// <summary>Makes <paramref name="team"/> the user's team; an unknown team is refused.</summary>
	public async Task<Result<Unit>> PickUserTeamAsync(TeamID team, CancellationToken cancellationToken = default)
	{
		var picked = await writer.ExecuteAsync("League.PickUserTeam", async (uow, ct) =>
		{
			var abbreviation = await uow.Db.Teams
				.Where(t => t.TeamID == team)
				.Select(t => t.Abbreviation)
				.SingleOrDefaultAsync(ct)
				.ConfigureAwait(false);

			if (abbreviation == null)
			{
				return Result.Fail<string>(LeagueErrors.UnknownTeam(team));
			}

			var info = await uow.Db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);
			info.UserTeamID = team;

			uow.Raise(new UserTeamPicked(team));
			return Result.Ok(abbreviation);
		}, cancellationToken).ConfigureAwait(false);

		if (picked.IsFailure)
		{
			return Result.Fail<Unit>(picked.Error!);
		}

		// After the commit, so a refused or rolled-back pick is never logged as made
		LeagueLog.TeamPicked(log, picked.Value);
		return Result.Ok();
	}
}

/// <summary>The expected ways the league's commands fail.</summary>
public static class LeagueErrors
{
	public static DomainError UnknownTeam(TeamID team) => new("UnknownTeam", $"There's no team {team.Value} in this league.");
}
