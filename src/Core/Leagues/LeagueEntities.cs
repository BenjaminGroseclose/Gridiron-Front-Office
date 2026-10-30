using GridironFrontOffice.Core.IDs;

namespace GridironFrontOffice.Core.Leagues;

public sealed class Conference
{
	public ConferenceID ConferenceID { get; set; }
	public string Key { get; set; } = "";
	public string Name { get; set; } = "";
}

public sealed class Division
{
	public DivisionID DivisionID { get; set; }
	public ConferenceID ConferenceID { get; set; }
	public string Key { get; set; } = "";
	public string Name { get; set; } = "";
}

/// <summary>A team, with its stadium as columns (D-078 #6). No metro and no strength: nothing reads them.</summary>
public sealed class Team
{
	public TeamID TeamID { get; set; }
	public DivisionID DivisionID { get; set; }
	public string Abbreviation { get; set; } = "";
	public string City { get; set; } = "";
	public string Nickname { get; set; } = "";

	/// <summary>
	/// #RRGGBB, for example
	/// </summary>
	public string PrimaryColor { get; set; } = "";
	public string SecondaryColor { get; set; } = "";
	public string AccentColor { get; set; } = "";
	public CrestShape CrestShape { get; set; }
	public string CrestMonogram { get; set; } = "";
	public CrestStripe CrestStripe { get; set; }
	public string StadiumName { get; set; } = "";
	public int StadiumCapacity { get; set; }
	public Roof StadiumRoof { get; set; }
	public Surface StadiumSurface { get; set; }
	public ClimateZone StadiumClimate { get; set; }
}

public sealed class College
{
	public CollegeID CollegeID { get; set; }
	public string Name { get; set; } = "";
	public CollegeRegion Region { get; set; }
	public CollegeTier Tier { get; set; }
}

/// <summary>
/// One row per season (D-089): the cap now; M3 and M4 add the rest.
/// </summary>
public sealed class LeagueSeason
{
	public int Season { get; set; }

	/// <summary>
	/// The salary cap for the given season
	/// </summary>
	public long Cap { get; set; }
}