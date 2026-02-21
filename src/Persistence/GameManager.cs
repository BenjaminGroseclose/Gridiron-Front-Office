using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using SQLite;
using SQLitePCL;

namespace GridironFrontOffice.Persistence;

public class GameManager
{
	private static readonly object SqliteInitLock = new();
	private static bool _sqliteInitialized;
	private readonly string _baseSavePath;

	public SQLiteConnection? CurrentConnection { get; private set; }

	public GameManager(string folder = "GFO_Saves")
	{
		EnsureSqliteInitialized();

		_baseSavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);

		if (!Directory.Exists(_baseSavePath))
		{
			Directory.CreateDirectory(_baseSavePath);
		}
	}

	private static void EnsureSqliteInitialized()
	{
		if (_sqliteInitialized)
		{
			return;
		}

		lock (SqliteInitLock)
		{
			if (_sqliteInitialized)
			{
				return;
			}

			Batteries_V2.Init();
			_sqliteInitialized = true;
		}
	}

	/// <summary>
	/// Creates a new game save with the specified name.
	/// </summary>
	/// <param name="saveName">The file name for the new game save.</param>
	/// <exception cref="DomainException">When a save with the specified name already exists.</exception>
	public void CreateNewGame(string saveName)
	{
		string fullPath = Path.Combine(_baseSavePath, $"{saveName}.db");

		if (File.Exists(fullPath))
		{
			var ex = new DomainException("A save with this name already exists.", "SAVE_ALREADY_EXISTS", isFatal: false, retryable: false);
			ex.Details.Add("SaveName", saveName);
			throw ex;
		}

		CurrentConnection = new SQLiteConnection(fullPath);

		InitializeDatabaseSchema();

		// Speed boost: Enable Write-Ahead Logging
		CurrentConnection.Execute("PRAGMA journal_mode=WAL;");

		// Enforce foreign key constraints for this connection
		CurrentConnection.Execute("PRAGMA foreign_keys = ON;");
	}

	public void LoadGame(string saveName)
	{
		string fullPath = GetSavePath(saveName);
		if (!File.Exists(fullPath))
		{
			var ex = new DomainException("Save not found", "SAVE_NOT_FOUND", isFatal: false, retryable: false);
			ex.Details.Add("SaveName", saveName);
			throw ex;
		}

		CurrentConnection = new SQLiteConnection(fullPath);

		// Speed boost: Enable Write-Ahead Logging
		CurrentConnection.Execute("PRAGMA journal_mode=WAL;");

		// Enforce foreign key constraints for this connection
		CurrentConnection.Execute("PRAGMA foreign_keys = ON;");
	}

	private string GetSavePath(string saveName)
	{
		return Path.Combine(_baseSavePath, $"{saveName}.db");
	}

	private void InitializeDatabaseSchema()
	{
		if (CurrentConnection == null)
		{
			throw new InvalidOperationException("No active database connection.");
		}

		CurrentConnection.CreateTable<League>();
		CurrentConnection.CreateTable<Player>();
		CurrentConnection.CreateTable<Stadium>();
		CurrentConnection.CreateTable<Conference>();
		CurrentConnection.CreateTable<Division>();
		CurrentConnection.CreateTable<Team>();
	}
}