using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.Session.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekAdvancePendingToGameRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WeekAdvancePending",
                table: "GameRooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeekAdvancePending",
                table: "GameRooms");
        }
    }
}
