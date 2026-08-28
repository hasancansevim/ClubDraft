using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLineupJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LineupJson",
                table: "Clubs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineupJson",
                table: "Clubs");
        }
    }
}
