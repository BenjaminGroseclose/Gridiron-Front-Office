using System.Globalization;
using GridironFrontOffice.Core.IDs;

namespace GridironFrontOffice.UI.Menu;

public sealed class Routes
{
	/// <summary>The open league's home.</summary>
	public const string Home = "/home";

	/// <summary>The league page.</summary>
	public const string League = "/league";

	/// <summary>The roster page.</summary>
	public const string Roster = "/roster";

	/// <summary>The main menu.</summary>
	public const string MainMenu = "/";

	/// <summary>The team picker: where an open league without a user team lands (F-01 §4).</summary>
	public const string TeamPicker = "/pick-team";

	/// <summary>New League's first step.</summary>
	public const string Wizard = "/new";

	/// <summary>The settings page.</summary>
	public const string Settings = "/settings";

	/// <summary>
	/// New League's last step (D-099): the league and the marked team, where Start picks it. The league's settings
	/// join it with their milestones.
	/// </summary>
	public const string Finalize = "/finalize";

	/// <summary>A team's page.</summary>
	public static string Team(TeamID team) => string.Create(CultureInfo.InvariantCulture, $"/team/{team.Value}");

	/// <summary>A route with the marked team: the summary shows it, and the picker marks it again.</summary>
	public static string WithTeam(string route, TeamID team) =>
		$"{route}?team={team.Value.ToString(CultureInfo.InvariantCulture)}";

}