using GridironFrontOffice.Domain;
using GridironFrontOffice.Persistence.Interfaces;
using System.Reflection;
using System.Text.Json;

namespace GridironFrontOffice.Persistence;

/// <summary>
/// Service responsible for loading default seed data from JSON resources
/// </summary>
public class SeedDataService : ISeedDataService
{
	private const string RESOURCE_NAME = "GridironFrontOffice.Persistence.Resources.default_data.json";

	/// <summary>
	/// Loads the default data from the embedded JSON resource file
	/// </summary>
	public async Task<(IEnumerable<Team> Teams, IEnumerable<Stadium> Stadiums, IEnumerable<Conference> Conferences, IEnumerable<Division> Divisions)> LoadDefaultDataAsync()
	{
		try
		{
			var assembly = typeof(SeedDataService).GetTypeInfo().Assembly;
			using (var stream = assembly.GetManifestResourceStream(RESOURCE_NAME))
			{
				if (stream == null)
				{
					throw new FileNotFoundException($"Embedded resource not found: {RESOURCE_NAME}");
				}

				using (var reader = new StreamReader(stream))
				{
					var json = await reader.ReadToEndAsync();
					var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					var data = JsonSerializer.Deserialize<SeedData>(json, options);

					return (
						data?.Teams ?? [],
						data?.Stadiums ?? [],
						data?.Conferences ?? [],
						data?.Divisions ?? []
					);
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
