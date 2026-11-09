using System.Globalization;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.UI.Components.Overlays;
using Microsoft.AspNetCore.Components;

namespace GridironFrontOffice.UI.Menu;

/// <summary>
/// What the main menu, the load screen and the shell do with saves (M0 WP9, PERSISTENCE.md §9–10): the one place that
/// turns ISaveManager's results into navigation and messages, so every screen says the same thing.
/// </summary>
public sealed class SaveActions(ISaveManager saves, IGfoDialogs dialogs, IGfoToasts toasts, NavigationManager navigation, LeagueQueries queries, CreateLeague create, LeagueCommands commands)
{
	/// <summary>The open league's home.</summary>
	public const string LeagueHome = "/league";

	/// <summary>The main menu.</summary>
	public const string MainMenu = "/";

	/// <summary>The team picker: where an open league without a user team lands (F-01 §4).</summary>
	public const string TeamPicker = "/pick-team";

	/// <summary>New League's first step.</summary>
	public const string Wizard = "/new";

	/// <summary>
	/// New League's last step (D-099): the league and the marked team, where Start picks it. The league's settings
	/// join it with their milestones.
	/// </summary>
	public const string Finalize = "/finalize";

	/// <summary>A route with the marked team: the summary shows it, and the picker marks it again.</summary>
	public static string WithTeam(string route, TeamID team) =>
		$"{route}?team={team.Value.ToString(CultureInfo.InvariantCulture)}";

	/// <summary>Opens a save and goes to its home. A failure is reported and returns false.</summary>
	public async Task<bool> OpenAsync(SaveCard card)
	{
		ArgumentNullException.ThrowIfNull(card);
		var opened = await saves.OpenAsync(card.FilePath);
		if (opened.IsFailure)
		{
			await toasts.ErrorAsync($"Couldn't open {SaveCardText.Title(card)}", opened.Error!.Message);
			return false;
		}

		if (opened.Value.Upgraded)
		{
			await toasts.InfoAsync($"{opened.Value.LeagueName} was upgraded", "It was saved by an older version. A snapshot of it was kept first.");
		}

		navigation.NavigateTo(await LandingAsync());
		return true;
	}

	/// <summary>Where the open league lands: the picker until it has a user team, then its home.</summary>
	public async Task<string> LandingAsync()
	{
		var header = await Task.Run(() => queries.GetHeaderAsync());
		return header.UserTeam is null ? TeamPicker : LeagueHome;
	}

	/// <summary>
	/// Generates the league and opens its picker, or returns why it was refused.
	/// </summary>
	public async Task<DomainError?> CreateLeagueAsync(NewLeague league, IProgress<LeagueCreationStep> progress)
	{
		var created = await create.RunAsync(league, progress);
		if (created.IsFailure)
		{
			return created.Error;
		}

		navigation.NavigateTo(TeamPicker);
		return null;
	}

	/// <summary>
	/// Makes the marked team the user's and opens the league's home (D-095 #3, D-099: the summary's Start is the
	/// confirmation). True when picked.
	/// </summary>
	public async Task<bool> PickAsync(TeamIdentityDto team)
	{
		ArgumentNullException.ThrowIfNull(team);
		var picked = await commands.PickUserTeamAsync(team.TeamID);
		if (picked.IsFailure)
		{
			await toasts.ErrorAsync($"Couldn't pick the {team.City} {team.Nickname}", picked.Error!.Message);
			return false;
		}

		navigation.NavigateTo(LeagueHome);
		return true;
	}

	/// <summary>
	/// Asks first, naming the league, then sends the save and its snapshots to the Recycle Bin. True when it's gone.
	/// </summary>
	public async Task<bool> DeleteAsync(SaveCard card)
	{
		ArgumentNullException.ThrowIfNull(card);
		var title = SaveCardText.Title(card);
		// An unreadable file has no SaveID, so no snapshots go with it (PERSISTENCE.md §10).
		var what = card.Status == SaveCardStatus.Unreadable ? title : $"{title}, with its snapshots,";
		var confirmed = await dialogs.ConfirmAsync(
			$"Delete {title}?",
			$"{what} goes to the Recycle Bin. You can restore it from there.",
			confirm: "Delete",
			cancel: "Keep it");
		if (!confirmed)
		{
			return false;
		}

		var deleted = await saves.DeleteAsync(card.FilePath);
		if (deleted.IsSuccess)
		{
			await toasts.SuccessAsync($"{title} moved to the Recycle Bin");
			return true;
		}

		var error = deleted.Error!;
		if (error.Code == SaveErrors.DeleteCancelled(card.FileName).Code)
		{
			await toasts.InfoAsync(error.Message);
		}
		else
		{
			await toasts.ErrorAsync($"Couldn't delete {title}", error.Message);
		}

		return false;
	}

	/// <summary>Closes the open league and goes back to the main menu.</summary>
	public async Task CloseAsync()
	{
		await saves.CloseAsync();
		navigation.NavigateTo(MainMenu);
	}
}
