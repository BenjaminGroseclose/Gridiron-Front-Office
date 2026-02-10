using SQLite;

namespace GridironFrontOffice.Persistence.Interfaces;

/// <summary>
/// Provides access to the SQLite database connection.
/// </summary>
public interface IDatabaseConnectionFactory
{
	/// <summary>
	/// Gets the current active database connection.
	/// </summary>
	/// <returns>The active SQLiteConnection, or null if no connection is active.</returns>
	SQLiteConnection? GetConnection();

	/// <summary>
	/// Indicates whether a database connection is currently active.
	/// </summary>
	bool HasActiveConnection { get; }
}