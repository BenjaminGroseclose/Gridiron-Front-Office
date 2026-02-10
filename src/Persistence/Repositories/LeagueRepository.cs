using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;
using SQLite;

namespace GridironFrontOffice.Persistence.Repositories;

public class LeagueRepository : BaseRepository<League>, IBaseRepository<League>
{
	public LeagueRepository(IDatabaseConnectionFactory databaseConnectionFactory) : base(databaseConnectionFactory)
	{
	}

	public Task<bool> DeleteAsync(int entityId)
	{
		GetConnection().Delete<League>(entityId);
		return Task.FromResult(true);
	}

	public async Task<IEnumerable<League>> GetAllAsync()
	{
		var leagues = GetConnection().Table<League>();

		var result = leagues.AsEnumerable();

		return await Task.FromResult(result);
	}

	public Task<League?> GetByIdAsync(int entityId)
	{
		var league = GetConnection().Table<League>().FirstOrDefault(l => l.LeagueID == entityId);
		return Task.FromResult(league);
	}

	public Task<int> InsertAsync(League entity)
	{
		var result = GetConnection().Insert(entity);

		if (result == 0)
		{
			var ex = new DomainException("Failed to insert League entity.", "LEAGUE_INSERT_FAILED", isFatal: false, retryable: true);
			ex.Details.Add("LeagueName", entity.Name);
			throw ex;
		}

		return Task.FromResult(entity.LeagueID);
	}

	public Task<int> UpdateAsync(League entity)
	{
		var rowsUpdated = GetConnection().Update(entity);
		return Task.FromResult(rowsUpdated);
	}
}