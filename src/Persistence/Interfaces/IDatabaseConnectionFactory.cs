namespace GridironFrontOffice.Persistence.Interfaces;

/// <summary>
/// Provides access to the active game's Entity Framework database context.
/// </summary>
public interface IDatabaseConnectionFactory
{
	/// <summary>
	/// Creates a database context bound to the active game save.
	/// </summary>
	GridironFrontOfficeDbContext CreateDbContext();

	/// <summary>
	/// Indicates whether a database connection is currently active.
	/// </summary>
	bool HasActiveConnection { get; }
}