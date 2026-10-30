using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class DivisionConfiguration : IEntityTypeConfiguration<Division>
{
	public void Configure(EntityTypeBuilder<Division> builder)
	{
		builder.ToTable("Divisions");
		builder.HasKey(x => x.DivisionID);
		builder.HasOne<Conference>().WithMany().HasForeignKey(x => x.ConferenceID);
	}
}
