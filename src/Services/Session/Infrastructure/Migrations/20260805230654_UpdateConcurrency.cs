using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.Session.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GameRooms");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "GameRooms",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "GameRooms");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GameRooms",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
