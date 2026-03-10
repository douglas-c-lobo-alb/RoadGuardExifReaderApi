using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RoadTurbulence202603100918 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadTurbulence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    RoadTurbulenceType = table.Column<int>(type: "INTEGER", nullable: false),
                    HexagonId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadTurbulence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadTurbulence_Hexagons_HexagonId",
                        column: x => x.HexagonId,
                        principalTable: "Hexagons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadTurbulence_HexagonId",
                table: "RoadTurbulence",
                column: "HexagonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadTurbulence");
        }
    }
}
