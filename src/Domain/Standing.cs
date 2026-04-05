using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Application;

public class Standing
{
	public Team Team { get; set; }
	public int Wins { get; set; }
	public int Losses { get; set; }
	public int Ties { get; set; }
	public int Ranking { get; set; }
}