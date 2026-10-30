using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

/// <summary>One row per season (D-089). The key is the season itself, so it's never generated.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class LeagueSeasonConfiguration : IEntityTypeConfiguration<LeagueSeason>
{
	public void Configure(EntityTypeBuilder<LeagueSeason> builder)
	{
		builder.ToTable("Seasons");
		builder.HasKey(x => x.Season);

		// EF's SQLite convention makes a lone int key AUTOINCREMENT otherwise (WP5 §9)
		builder.Property(x => x.Season).ValueGeneratedNever();
	}
}
