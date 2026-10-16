using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    Week = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeScore = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayScore = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaveInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueName = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentSeason = table.Column<int>(type: "INTEGER", nullable: false),
                    Generation = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Abbreviation = table.Column<string>(type: "TEXT", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Losses = table.Column<int>(type: "INTEGER", nullable: false),
                    Ties = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsFor = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsAgainst = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSeasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamePlayLogs",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    Compression = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayLogs", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_GamePlayLogs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<string>(type: "TEXT", nullable: false),
                    Overall = table.Column<int>(type: "INTEGER", nullable: false),
                    IsInjured = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalValue = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contracts_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGameStats",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat01 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat02 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat03 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat04 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat05 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat06 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat07 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat08 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat09 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat10 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat11 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat12 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat13 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat14 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat15 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat16 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat17 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat18 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat19 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat20 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat21 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat22 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat23 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat24 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat25 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat26 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat27 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat28 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat29 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat30 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat31 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat32 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat33 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat34 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat35 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat36 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat37 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat38 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat39 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat40 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat41 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat42 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat43 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat44 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat45 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat46 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat47 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat48 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat49 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat50 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat51 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat52 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat53 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat54 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat55 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat56 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat57 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat58 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat59 = table.Column<int>(type: "INTEGER", nullable: false),
                    Stat60 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGameStats", x => new { x.GameId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_PlayerGameStats_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerGameStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseSalary = table.Column<long>(type: "INTEGER", nullable: false),
                    ProratedBonus = table.Column<long>(type: "INTEGER", nullable: false),
                    CapHit = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractYears_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PlayerId",
                table: "Contracts",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TeamId",
                table: "Contracts",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractYears_ContractId_Season",
                table: "ContractYears",
                columns: new[] { "ContractId", "Season" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Season_Week",
                table: "Games",
                columns: new[] { "Season", "Week" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameStats_PlayerId",
                table: "PlayerGameStats",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId_Status",
                table: "Players",
                columns: new[] { "TeamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Abbreviation",
                table: "Teams",
                column: "Abbreviation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_TeamId_Season",
                table: "TeamSeasons",
                columns: new[] { "TeamId", "Season" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractYears");

            migrationBuilder.DropTable(
                name: "GamePlayLogs");

            migrationBuilder.DropTable(
                name: "PlayerGameStats");

            migrationBuilder.DropTable(
                name: "SaveInfo");

            migrationBuilder.DropTable(
                name: "TeamSeasons");

            migrationBuilder.DropTable(
                name: "TransactionLog");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
