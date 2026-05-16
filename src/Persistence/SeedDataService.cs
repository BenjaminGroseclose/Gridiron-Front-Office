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

	public SeedDataService(IBaseRepository<Team> teamRepository, IBaseRepository<Stadium> stadiumRepository, IBaseRepository<Season> seasonRepository, IBaseRepository<DraftPick> draftPickRepository)
	{
		_teamRepository = teamRepository;
		_stadiumRepository = stadiumRepository;
		_seasonRepository = seasonRepository;
		_draftPickRepository = draftPickRepository;
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

					var draftPicks = GenerateDraftPicks(data.Teams, startYear);
					await _draftPickRepository.BulkInsertAsync(draftPicks);

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

		for (int i = -ARCHIVED_SEASON_LOOKBACK; i <= 10; i++)
		{
			int seasonID = startYear + i;
			var isArchived = i < 0;

			seasons.Add(new Season
			{
				ID = seasonID,
				Status = isArchived ? SeasonStatus.Archived : SeasonStatus.NotStarted,
				IsCurrentSeason = false,
				StartDate = new DateOnly(seasonID, 3, 1), // March 1st of the season year
				RegularSeasonStartDate = this.GetFirstSunday(seasonID, 9),
				EndDate = new DateOnly(seasonID + 1, 2, 28) // February 28th of the following year
			});
		}

		return seasons;
	}

	private const int DRAFT_PICK_SEASON_COUNT = 5;

	private List<DraftPick> GenerateDraftPicks(List<Team> teams, int startYear)
	{
		var draftPicks = new List<DraftPick>();

		for (int i = startYear; i < startYear + DRAFT_PICK_SEASON_COUNT; i++)
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
						TeamID = team.ID,
					});
				}
			}
		}

		return draftPicks;
	}

	/// <summary>
	/// Helper method to find the first Sunday of a given month and year, used for setting regular season start dates in generated seasons
	/// </summary>
	/// <param name="year">The year to find the first Sunday in.</param>
	/// <param name="month">The month to find the first Sunday in.</param>
	/// <returns>The first Sunday of the specified month and year as a DateOnly.</returns>
	private DateOnly GetFirstSunday(int year, int month)
	{
		DateTime firstDayOfMonth = new DateTime(year, month, 1);
		DateTime firstSunday = firstDayOfMonth;

		while (firstSunday.DayOfWeek != DayOfWeek.Sunday)
		{
			firstSunday = firstSunday.AddDays(1);
		}

		return new DateOnly(firstSunday.Year, firstSunday.Month, firstSunday.Day);
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
