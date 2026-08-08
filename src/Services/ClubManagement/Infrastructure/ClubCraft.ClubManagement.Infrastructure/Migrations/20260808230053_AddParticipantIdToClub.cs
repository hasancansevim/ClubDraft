using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubCraft.ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantIdToClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParticipantId",
                table: "Clubs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParticipantId",
                table: "Clubs");
        }
    }
}
