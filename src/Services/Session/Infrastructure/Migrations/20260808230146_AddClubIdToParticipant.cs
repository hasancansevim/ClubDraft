using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.Session.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClubIdToParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Participant",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Participant");
        }
    }
}
