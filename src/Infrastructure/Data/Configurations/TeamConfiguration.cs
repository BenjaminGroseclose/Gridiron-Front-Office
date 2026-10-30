using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
	public void Configure(EntityTypeBuilder<Team> builder)
	{
		builder.ToTable("Teams");
		builder.HasKey(x => x.TeamID);
		builder.HasOne<Division>().WithMany().HasForeignKey(x => x.DivisionID);
	}
}
