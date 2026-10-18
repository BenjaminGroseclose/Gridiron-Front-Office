using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Core.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridironFrontOffice.Infrastructure.Data.Configurations;

/// <summary>PERSISTENCE.md §5.1: the single metadata row.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Found by ApplyConfigurationsFromAssembly.")]
internal sealed class SaveInfoConfiguration : IEntityTypeConfiguration<SaveInfo>
{
    public void Configure(EntityTypeBuilder<SaveInfo> builder)
    {
        builder.ToTable("SaveInfo");
        builder.HasKey(x => x.SaveID);
        builder.Property(x => x.SaveID).ValueGeneratedNever();
        builder.Property(x => x.LeagueName).IsRequired();
        builder.Property(x => x.AppVersionCreated).IsRequired();
        builder.Property(x => x.AppVersionLastOpened).IsRequired();
    }
}
