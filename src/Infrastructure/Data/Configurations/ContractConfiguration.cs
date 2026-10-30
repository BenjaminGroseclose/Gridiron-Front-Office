using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
	public void Configure(EntityTypeBuilder<Contract> builder)
	{
		builder.ToTable("Contracts");
		builder.HasKey(x => x.ContractID);
		builder.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerID);
		builder.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamID);
	}
}
