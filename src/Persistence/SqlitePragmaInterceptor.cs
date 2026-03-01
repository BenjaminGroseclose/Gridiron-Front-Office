using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace GridironFrontOffice.Persistence;

/// <summary>
/// Automatically applies required SQLite pragmas on every new connection.
/// SQLite does not enforce foreign keys by default — this interceptor opts in
/// on every connection regardless of which code path creates the DbContext.
/// </summary>
internal sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
	public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
	{
		ApplyPragmas(connection);
	}

	public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
	{
		await ApplyPragmasAsync(connection, cancellationToken);
	}

	private static void ApplyPragmas(DbConnection connection)
	{
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "PRAGMA foreign_keys = ON;";
		cmd.ExecuteNonQuery();
	}

	private static async Task ApplyPragmasAsync(DbConnection connection, CancellationToken cancellationToken)
	{
		await using var cmd = connection.CreateCommand();
		cmd.CommandText = "PRAGMA foreign_keys = ON;";
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}
}
