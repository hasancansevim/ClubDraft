using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClubCraft.MatchEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignClubPowerRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalOverall",
                table: "ClubPowerRatings",
                newName: "Moral");

            migrationBuilder.AddColumn<string>(
                name: "Formation",
                table: "ClubPowerRatings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LineupSlotAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlotId = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClubPowerRatingClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineupSlotAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineupSlotAssignment_ClubPowerRatings_ClubPowerRatingClubId",
                        column: x => x.ClubPowerRatingClubId,
                        principalTable: "ClubPowerRatings",
                        principalColumn: "ClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterPlayerSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Overall = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false),
                    ClubPowerRatingClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterPlayerSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterPlayerSnapshot_ClubPowerRatings_ClubPowerRatingClubId",
                        column: x => x.ClubPowerRatingClubId,
                        principalTable: "ClubPowerRatings",
                        principalColumn: "ClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineupSlotAssignment_ClubPowerRatingClubId",
                table: "LineupSlotAssignment",
                column: "ClubPowerRatingClubId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterPlayerSnapshot_ClubPowerRatingClubId",
                table: "RosterPlayerSnapshot",
                column: "ClubPowerRatingClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineupSlotAssignment");

            migrationBuilder.DropTable(
                name: "RosterPlayerSnapshot");

            migrationBuilder.DropColumn(
                name: "Formation",
                table: "ClubPowerRatings");

            migrationBuilder.RenameColumn(
                name: "Moral",
                table: "ClubPowerRatings",
                newName: "TotalOverall");
        }
    }
}
