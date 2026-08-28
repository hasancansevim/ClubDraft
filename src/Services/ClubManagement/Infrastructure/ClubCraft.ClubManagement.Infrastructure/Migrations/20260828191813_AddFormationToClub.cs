using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormationToClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Formation",
                table: "Clubs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Formation",
                table: "Clubs");
        }
    }
}
