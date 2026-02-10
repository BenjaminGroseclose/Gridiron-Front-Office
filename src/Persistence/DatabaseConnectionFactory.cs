using GridironFrontOffice.Persistence.Interfaces;
using SQLite;

namespace GridironFrontOffice.Persistence;

/// <summary>
/// Provides access to the current game's database connection.
/// Delegates connection lifecycle management to GameManager.
/// </summary>
public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
	private readonly GameManager _gameManager;

	public DatabaseConnectionFactory(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public SQLiteConnection? GetConnection()
	{
		return _gameManager.CurrentConnection;
	}

	public bool HasActiveConnection => _gameManager.CurrentConnection != null;
}