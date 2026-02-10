using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;
using SQLite;

namespace GridironFrontOffice.Persistence.Repositories;

public class BaseRepository<T> where T : BaseEntity
{
	private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

	public BaseRepository(IDatabaseConnectionFactory databaseConnectionFactory)
	{
		_databaseConnectionFactory = databaseConnectionFactory;
	}

	private SQLiteConnection? Connection => _databaseConnectionFactory.GetConnection();

	protected SQLiteConnection GetConnection()
	{
		if (_databaseConnectionFactory.HasActiveConnection == false || Connection == null)
		{
			throw new DomainException("No active database connection.", "NO_ACTIVE_DB_CONNECTION", isFatal: true, retryable: false);
		}

		return Connection;
	}
}