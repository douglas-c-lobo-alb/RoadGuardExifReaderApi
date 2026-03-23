using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "Images",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_AgentId",
                table: "Images",
                column: "AgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Agents_AgentId",
                table: "Images",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Agents_AgentId",
                table: "Images");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Images_AgentId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Images");
        }
    }
}
