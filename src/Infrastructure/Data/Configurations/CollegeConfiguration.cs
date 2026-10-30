using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class CollegeConfiguration : IEntityTypeConfiguration<College>
{
	public void Configure(EntityTypeBuilder<College> builder)
	{
		builder.ToTable("Colleges");
		builder.HasKey(x => x.CollegeID);
	}
}
