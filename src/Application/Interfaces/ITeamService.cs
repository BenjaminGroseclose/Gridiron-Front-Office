using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Application.Interfaces;

public interface ITeamService
{
	Task<IEnumerable<Team>> GetAllTeamsAsync();
	Task<Team> GetTeam(int teamID);

	Task<List<Standing>> GetTeamStandings(int seasonID);
	Task<List<Standing>> GetTeamStandings(int seasonID, Conference conference);
	Task<List<Standing>> GetTeamStandings(int seasonID, Conference conference, Division division);
}