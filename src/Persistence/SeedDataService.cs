using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Persistence.Interfaces;
using GridironFrontOffice.Persistence.Models;
using System.Reflection;
using System.Text.Json;

namespace GridironFrontOffice.Persistence;

/// <summary>
/// Service responsible for loading default seed data from JSON resources
/// </summary>
public class SeedDataService : ISeedDataService
{
	private const string DEFAULT_DATA_JSON_RESOURCE = "GridironFrontOffice.Persistence.Resources.default_data.json";
	private const string NAME_POOL_JSON_RESOURCE = "GridironFrontOffice.Persistence.Resources.name_pool.json";
	private const string ARCHETYPE_JSON_RESOURCE = "GridironFrontOffice.Persistence.Resources.player_archetypes.json";

	private NamePool _namePool = null;
	private Dictionary<PlayerPosition, List<PlayerArchetype>> _playerArchetypes = null;

	private readonly IBaseRepository<Team> _teamRepository;
	private readonly IBaseRepository<Stadium> _stadiumRepository;
	private readonly IBaseRepository<Season> _seasonRepository;
	private readonly IBaseRepository<DraftPick> _draftPickRepository;

	public SeedDataService(IBaseRepository<Team> teamRepository, IBaseRepository<Stadium> stadiumRepository, IBaseRepository<Season> seasonRepository)
	{
		_teamRepository = teamRepository;
		_stadiumRepository = stadiumRepository;
		_seasonRepository = seasonRepository;
	}

	/// <inheritdoc/>
	public async Task<bool> LoadDefaultDataAsync(int startYear)
	{
		try
		{
			var assembly = typeof(SeedDataService).GetTypeInfo().Assembly;
			using (var stream = assembly.GetManifestResourceStream(DEFAULT_DATA_JSON_RESOURCE))
			{
				if (stream == null)
				{
					throw new FileNotFoundException($"Embedded resource not found: {DEFAULT_DATA_JSON_RESOURCE}");
				}

				using (var reader = new StreamReader(stream))
				{
					var json = await reader.ReadToEndAsync();
					var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					var data = JsonSerializer.Deserialize<SeedData>(json, options);

					await _stadiumRepository.BulkInsertAsync(data.Stadiums);
					await _teamRepository.BulkInsertAsync(data.Teams);

					var season = this.GenerateInitialSeasons(startYear);

					await _seasonRepository.BulkInsertAsync(season);

					return true;
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to load seed data from embedded resource", ex);
		}
	}

	/// <inheritdoc/>
	public async Task<NamePool> LoadNamePoolAsync()
	{
		if (this._namePool != null)
		{
			return this._namePool; // Return cached name pool if already loaded
		}

		try
		{
			var assembly = typeof(SeedDataService).GetTypeInfo().Assembly;
			using (var stream = assembly.GetManifestResourceStream(NAME_POOL_JSON_RESOURCE))
			{
				if (stream == null)
				{
					throw new FileNotFoundException($"Embedded resource not found: {NAME_POOL_JSON_RESOURCE}");
				}

				using (var reader = new StreamReader(stream))
				{
					var json = await reader.ReadToEndAsync();
					var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					var data = JsonSerializer.Deserialize<NamePool>(json, options);

					this._namePool = data; // Cache the name pool for future use

					return data ?? new NamePool();
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to load seed data from embedded resource", ex);
		}
	}

	/// <inheritdoc/>
	public async Task<Dictionary<PlayerPosition, List<PlayerArchetype>>> LoadPlayerArchetypesAsync()
	{
		if (this._playerArchetypes != null)
		{
			return this._playerArchetypes; // Return cached name pool if already loaded
		}

		try
		{
			var assembly = typeof(SeedDataService).GetTypeInfo().Assembly;
			using (var stream = assembly.GetManifestResourceStream(ARCHETYPE_JSON_RESOURCE))
			{
				if (stream == null)
				{
					throw new FileNotFoundException($"Embedded resource not found: {ARCHETYPE_JSON_RESOURCE}");
				}

				using (var reader = new StreamReader(stream))
				{
					var json = await reader.ReadToEndAsync();
					var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					var data = JsonSerializer.Deserialize<Dictionary<PlayerPosition, List<PlayerArchetype>>>(json, options);

					this._playerArchetypes = data; // Cache the name pool for future use

					return data ?? new Dictionary<PlayerPosition, List<PlayerArchetype>>();
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to load seed data from embedded resource", ex);
		}
	}

	private const int ARCHIVED_SEASON_LOOKBACK = 5;

	private List<Season> GenerateInitialSeasons(int startYear)
	{
		var seasons = new List<Season>();

		for (int i = -ARCHIVED_SEASON_LOOKBACK; i < 10; i++)
		{
			int seasonID = startYear + i;
			var isArchived = i < 0;

			seasons.Add(new Season
			{
				SeasonID = seasonID,
				Status = isArchived ? SeasonStatus.Archived : SeasonStatus.NotStarted,
				IsCurrentSeason = false,
				StartDate = new DateOnly(seasonID, 3, 1), // March 1st of the season year
				RegularSeasonStartDate = new DateOnly(seasonID, 9, 1), // September 1st of the season year
				EndDate = new DateOnly(seasonID + 1, 2, 28) // February 28th of the following year
			});
		}

		return seasons;
	}

	private async Task SeedDraftPicks(int startYear)
	{
		var teams = await _teamRepository.GetAllAsync();
		var draftPicks = new List<DraftPick>();

		for (int i = startYear; i < startYear + 10; i++)
		{
			for (int round = 1; round <= 7; round++)
			{
				foreach (var team in teams)
				{
					draftPicks.Add(new DraftPick
					{
						SeasonID = i,
						Round = round,
						DraftPickType = DraftPickType.Regular,
						TeamID = team.TeamID,
					});
				}
			}
		}
	}

	/// <summary>
	/// Internal class for deserializing the JSON structure
	/// </summary>
	private class SeedData
	{
		public List<Team> Teams { get; set; } = [];
		public List<Stadium> Stadiums { get; set; } = [];
	}
}
