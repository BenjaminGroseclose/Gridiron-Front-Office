using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Persistence;

public class GameManager
{
	private readonly string _baseSavePath;

	public string? CurrentDatabasePath { get; private set; }

	public GameManager(string folder = "GFO_Saves")
	{
		_baseSavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);

		if (!Directory.Exists(_baseSavePath))
		{
			Directory.CreateDirectory(_baseSavePath);
		}
	}

	public bool HasActiveConnection => string.IsNullOrWhiteSpace(CurrentDatabasePath) == false;

	/// <summary>
	/// The directory where save files are stored. Useful for debugging the resolved path.
	/// </summary>
	public string SaveDirectory => _baseSavePath;

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

		CurrentDatabasePath = fullPath;

		InitializeDatabaseSchema();
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

		CurrentDatabasePath = fullPath;
	}

	private string GetSavePath(string saveName)
	{
		return Path.Combine(_baseSavePath, $"{saveName}.db");
	}

	private void InitializeDatabaseSchema()
	{
		if (CurrentDatabasePath == null)
		{
			throw new InvalidOperationException("No active database connection.");
		}

		using var context = CreateDbContext();
		bool created = context.Database.EnsureCreated();
	}

	public GridironFrontOfficeDbContext CreateDbContext()
	{
		if (CurrentDatabasePath == null)
		{
			throw new InvalidOperationException("No active database connection.");
		}

		var options = new DbContextOptionsBuilder<GridironFrontOfficeDbContext>()
			.UseSqlite($"Data Source={CurrentDatabasePath}")
			.AddInterceptors(new SqlitePragmaInterceptor())
			.Options;

		return new GridironFrontOfficeDbContext(options);
	}
}