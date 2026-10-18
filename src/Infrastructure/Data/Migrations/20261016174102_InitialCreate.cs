using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridironFrontOffice.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaveInfo",
                columns: table => new
                {
                    SaveId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeagueName = table.Column<string>(type: "TEXT", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    AppVersionCreated = table.Column<string>(type: "TEXT", nullable: false),
                    AppVersionLastOpened = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastPlayedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayTimeSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    IsIronman = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClockMode = table.Column<string>(type: "TEXT", nullable: false),
                    ClockPreset = table.Column<string>(type: "TEXT", nullable: false),
                    InjuryFrequency = table.Column<string>(type: "TEXT", nullable: false),
                    CareerCanEnd = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEdited = table.Column<bool>(type: "INTEGER", nullable: false),
                    Generation = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveInfo", x => x.SaveId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaveInfo");
        }
    }
}
