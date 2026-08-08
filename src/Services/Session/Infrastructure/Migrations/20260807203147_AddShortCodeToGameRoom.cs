using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.Session.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShortCodeToGameRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "GameRooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_GameRooms_ShortCode",
                table: "GameRooms",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameRooms_ShortCode",
                table: "GameRooms");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "GameRooms");
        }
    }
}
