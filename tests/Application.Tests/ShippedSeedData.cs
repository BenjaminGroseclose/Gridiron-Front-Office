using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Application.Tests;

/// <summary>The data files the app ships, through the real embedded loader, loaded once for every generator test.</summary>
internal static class ShippedSeedData
{
	public static ISeedData Instance { get; } = new EmbeddedSeedData(NullLogger<EmbeddedSeedData>.Instance);
}
