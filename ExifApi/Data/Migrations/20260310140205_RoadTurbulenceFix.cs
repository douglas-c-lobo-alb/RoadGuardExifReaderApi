using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RoadTurbulenceFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulence_Hexagons_HexagonId",
                table: "RoadTurbulence");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoadTurbulence",
                table: "RoadTurbulence");

            migrationBuilder.RenameTable(
                name: "RoadTurbulence",
                newName: "RoadTurbulences");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulence_HexagonId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_HexagonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoadTurbulences",
                table: "RoadTurbulences",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Hexagons_HexagonId",
                table: "RoadTurbulences",
                column: "HexagonId",
                principalTable: "Hexagons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Hexagons_HexagonId",
                table: "RoadTurbulences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoadTurbulences",
                table: "RoadTurbulences");

            migrationBuilder.RenameTable(
                name: "RoadTurbulences",
                newName: "RoadTurbulence");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_HexagonId",
                table: "RoadTurbulence",
                newName: "IX_RoadTurbulence_HexagonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoadTurbulence",
                table: "RoadTurbulence",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulence_Hexagons_HexagonId",
                table: "RoadTurbulence",
                column: "HexagonId",
                principalTable: "Hexagons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
