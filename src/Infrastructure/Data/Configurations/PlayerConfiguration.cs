using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
	public void Configure(EntityTypeBuilder<Player> builder)
	{
		builder.ToTable("Players");
		builder.HasKey(x => x.PlayerID);
		builder.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamID);
		builder.HasOne<Team>().WithMany().HasForeignKey(x => x.DraftTeamID);
		builder.HasOne<College>().WithMany().HasForeignKey(x => x.CollegeID);
	}
}
