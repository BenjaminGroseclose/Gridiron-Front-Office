using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerPassYards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CareerPassYards",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            // Data migration (PERSISTENCE.md §7): backfill deterministically from data already in the save.
            migrationBuilder.Sql(
                "UPDATE Players SET CareerPassYards = " +
                "(SELECT COALESCE(SUM(s.Stat01), 0) FROM PlayerGameStats s WHERE s.PlayerId = Players.Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareerPassYards",
                table: "Players");
        }
    }
}
