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
                name: "Colleges",
                columns: table => new
                {
                    CollegeID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    Tier = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colleges", x => x.CollegeID);
                });

            migrationBuilder.CreateTable(
                name: "Conferences",
                columns: table => new
                {
                    ConferenceID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conferences", x => x.ConferenceID);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    Cap = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Season);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    DivisionID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConferenceID = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.DivisionID);
                    table.ForeignKey(
                        name: "FK_Divisions_Conferences_ConferenceID",
                        column: x => x.ConferenceID,
                        principalTable: "Conferences",
                        principalColumn: "ConferenceID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DivisionID = table.Column<int>(type: "INTEGER", nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryColor = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryColor = table.Column<string>(type: "TEXT", nullable: false),
                    AccentColor = table.Column<string>(type: "TEXT", nullable: false),
                    CrestShape = table.Column<string>(type: "TEXT", nullable: false),
                    CrestMonogram = table.Column<string>(type: "TEXT", nullable: false),
                    CrestStripe = table.Column<string>(type: "TEXT", nullable: false),
                    StadiumName = table.Column<string>(type: "TEXT", nullable: false),
                    StadiumCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    StadiumRoof = table.Column<string>(type: "TEXT", nullable: false),
                    StadiumSurface = table.Column<string>(type: "TEXT", nullable: false),
                    StadiumClimate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamID);
                    table.ForeignKey(
                        name: "FK_Teams_Divisions_DivisionID",
                        column: x => x.DivisionID,
                        principalTable: "Divisions",
                        principalColumn: "DivisionID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamID = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CollegeID = table.Column<int>(type: "INTEGER", nullable: false),
                    HometownName = table.Column<string>(type: "TEXT", nullable: false),
                    HometownState = table.Column<string>(type: "TEXT", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    DraftSeason = table.Column<int>(type: "INTEGER", nullable: true),
                    DraftRound = table.Column<int>(type: "INTEGER", nullable: true),
                    DraftPick = table.Column<int>(type: "INTEGER", nullable: true),
                    DraftTeamID = table.Column<int>(type: "INTEGER", nullable: true),
                    JerseyNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Forty = table.Column<double>(type: "REAL", nullable: false),
                    TenSplit = table.Column<double>(type: "REAL", nullable: false),
                    ThreeCone = table.Column<double>(type: "REAL", nullable: false),
                    Shuttle = table.Column<double>(type: "REAL", nullable: false),
                    Bench = table.Column<double>(type: "REAL", nullable: false),
                    Vertical = table.Column<double>(type: "REAL", nullable: false),
                    BroadJump = table.Column<double>(type: "REAL", nullable: false),
                    ArmLength = table.Column<double>(type: "REAL", nullable: false),
                    HandSize = table.Column<double>(type: "REAL", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    Acceleration = table.Column<double>(type: "REAL", nullable: false),
                    Agility = table.Column<double>(type: "REAL", nullable: false),
                    Strength = table.Column<double>(type: "REAL", nullable: false),
                    Stamina = table.Column<double>(type: "REAL", nullable: false),
                    Toughness = table.Column<double>(type: "REAL", nullable: false),
                    Awareness = table.Column<double>(type: "REAL", nullable: false),
                    Discipline = table.Column<double>(type: "REAL", nullable: false),
                    ArmStrength = table.Column<double>(type: "REAL", nullable: false),
                    ShortAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    MediumAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    DeepAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    ThrowOnTheRun = table.Column<double>(type: "REAL", nullable: false),
                    PocketPresence = table.Column<double>(type: "REAL", nullable: false),
                    DecisionMaking = table.Column<double>(type: "REAL", nullable: false),
                    BallSecurity = table.Column<double>(type: "REAL", nullable: false),
                    Vision = table.Column<double>(type: "REAL", nullable: false),
                    Elusiveness = table.Column<double>(type: "REAL", nullable: false),
                    BreakTackle = table.Column<double>(type: "REAL", nullable: false),
                    Hands = table.Column<double>(type: "REAL", nullable: false),
                    RouteRunning = table.Column<double>(type: "REAL", nullable: false),
                    Release = table.Column<double>(type: "REAL", nullable: false),
                    ContestedCatch = table.Column<double>(type: "REAL", nullable: false),
                    PassBlock = table.Column<double>(type: "REAL", nullable: false),
                    RunBlock = table.Column<double>(type: "REAL", nullable: false),
                    LeadBlock = table.Column<double>(type: "REAL", nullable: false),
                    PassRushPower = table.Column<double>(type: "REAL", nullable: false),
                    PassRushFinesse = table.Column<double>(type: "REAL", nullable: false),
                    BlockShedding = table.Column<double>(type: "REAL", nullable: false),
                    Tackling = table.Column<double>(type: "REAL", nullable: false),
                    Pursuit = table.Column<double>(type: "REAL", nullable: false),
                    HitPower = table.Column<double>(type: "REAL", nullable: false),
                    ManCoverage = table.Column<double>(type: "REAL", nullable: false),
                    ZoneCoverage = table.Column<double>(type: "REAL", nullable: false),
                    Press = table.Column<double>(type: "REAL", nullable: false),
                    BallSkills = table.Column<double>(type: "REAL", nullable: false),
                    KickPower = table.Column<double>(type: "REAL", nullable: false),
                    KickAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    PuntPower = table.Column<double>(type: "REAL", nullable: false),
                    PuntAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    Return = table.Column<double>(type: "REAL", nullable: false),
                    SnapAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    Potential = table.Column<double>(type: "REAL", nullable: false),
                    DevelopmentCurve = table.Column<string>(type: "TEXT", nullable: false),
                    BustRisk = table.Column<double>(type: "REAL", nullable: false),
                    PeakAgeOffset = table.Column<double>(type: "REAL", nullable: false),
                    InjuryProneness = table.Column<double>(type: "REAL", nullable: false),
                    WorkEthic = table.Column<double>(type: "REAL", nullable: false),
                    Coachability = table.Column<double>(type: "REAL", nullable: false),
                    Greed = table.Column<double>(type: "REAL", nullable: false),
                    Loyalty = table.Column<double>(type: "REAL", nullable: false),
                    Competitiveness = table.Column<double>(type: "REAL", nullable: false),
                    Ego = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerID);
                    table.ForeignKey(
                        name: "FK_Players_Colleges_CollegeID",
                        column: x => x.CollegeID,
                        principalTable: "Colleges",
                        principalColumn: "CollegeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Players_Teams_DraftTeamID",
                        column: x => x.DraftTeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamID",
                        column: x => x.TeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaveInfo",
                columns: table => new
                {
                    SaveID = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    Generation = table.Column<long>(type: "INTEGER", nullable: false),
                    UserTeamID = table.Column<int>(type: "INTEGER", nullable: true),
                    Seed = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CurrentSeason = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPhase = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveInfo", x => x.SaveID);
                    table.ForeignKey(
                        name: "FK_SaveInfo_Teams_UserTeamID",
                        column: x => x.UserTeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    ContractID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerID = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamID = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    SignedSeason = table.Column<int>(type: "INTEGER", nullable: false),
                    Years = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalValue = table.Column<long>(type: "INTEGER", nullable: false),
                    SigningBonus = table.Column<long>(type: "INTEGER", nullable: false),
                    GuaranteedTotal = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractID);
                    table.ForeignKey(
                        name: "FK_Contracts_Players_PlayerID",
                        column: x => x.PlayerID,
                        principalTable: "Players",
                        principalColumn: "PlayerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Teams_TeamID",
                        column: x => x.TeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractYears",
                columns: table => new
                {
                    ContractID = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseSalary = table.Column<long>(type: "INTEGER", nullable: false),
                    ProratedBonus = table.Column<long>(type: "INTEGER", nullable: false),
                    GuaranteedAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    CapHit = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractYears", x => new { x.ContractID, x.Season });
                    table.ForeignKey(
                        name: "FK_ContractYears_Contracts_ContractID",
                        column: x => x.ContractID,
                        principalTable: "Contracts",
                        principalColumn: "ContractID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PlayerID",
                table: "Contracts",
                column: "PlayerID");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TeamID",
                table: "Contracts",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_ConferenceID",
                table: "Divisions",
                column: "ConferenceID");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CollegeID",
                table: "Players",
                column: "CollegeID");

            migrationBuilder.CreateIndex(
                name: "IX_Players_DraftTeamID",
                table: "Players",
                column: "DraftTeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamID_Status",
                table: "Players",
                columns: new[] { "TeamID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SaveInfo_UserTeamID",
                table: "SaveInfo",
                column: "UserTeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionID",
                table: "Teams",
                column: "DivisionID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractYears");

            migrationBuilder.DropTable(
                name: "SaveInfo");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Colleges");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "Conferences");
        }
    }
}
