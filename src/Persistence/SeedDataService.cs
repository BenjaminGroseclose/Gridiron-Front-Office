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

	/// <inheritdoc/>
	public async Task<(IEnumerable<Team> Teams, IEnumerable<Stadium> Stadiums)> LoadDefaultDataAsync()
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

					return (
						data?.Teams ?? [],
						data?.Stadiums ?? []
					);
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

	/// <summary>
	/// Internal class for deserializing the JSON structure
	/// </summary>
	private class SeedData
	{
		public List<Team> Teams { get; set; } = [];
		public List<Stadium> Stadiums { get; set; } = [];
		public List<Conference> Conferences { get; set; } = [];
		public List<Division> Divisions { get; set; } = [];
	}
}
