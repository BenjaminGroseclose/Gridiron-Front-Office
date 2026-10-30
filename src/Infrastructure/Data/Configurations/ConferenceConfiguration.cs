using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class ConferenceConfiguration : IEntityTypeConfiguration<Conference>
{
	public void Configure(EntityTypeBuilder<Conference> builder)
	{
		builder.ToTable("Conferences");
		builder.HasKey(x => x.ConferenceID);
	}
}
