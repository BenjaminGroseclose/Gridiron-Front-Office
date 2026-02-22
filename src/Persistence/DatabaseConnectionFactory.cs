using GridironFrontOffice.Persistence.Interfaces;

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

	public GridironFrontOfficeDbContext CreateDbContext()
	{
		return _gameManager.CreateDbContext();
	}

	public bool HasActiveConnection => _gameManager.HasActiveConnection;
}