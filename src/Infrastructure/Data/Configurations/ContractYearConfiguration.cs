using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class ContractYearConfiguration : IEntityTypeConfiguration<ContractYear>
{
	public void Configure(EntityTypeBuilder<ContractYear> builder)
	{
		builder.ToTable("ContractYears");
		builder.HasKey(x => new { x.ContractID, x.Season });
		builder.HasOne<Contract>().WithMany().HasForeignKey(x => x.ContractID);
	}
}
