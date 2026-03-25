using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeEntities_ImageTurbulence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_RoadTurbulences_RoadTurbulenceId",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Hexagons_HexagonId",
                table: "RoadTurbulences");

            migrationBuilder.DropIndex(
                name: "IX_Images_RoadTurbulenceId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "RoadTurbulenceId",
                table: "Images");

            migrationBuilder.RenameColumn(
                name: "HexagonId",
                table: "RoadTurbulences",
                newName: "ImageId");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_HexagonId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Images_ImageId",
                table: "RoadTurbulences",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Images_ImageId",
                table: "RoadTurbulences");

            migrationBuilder.RenameColumn(
                name: "ImageId",
                table: "RoadTurbulences",
                newName: "HexagonId");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_ImageId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_HexagonId");

            migrationBuilder.AddColumn<int>(
                name: "RoadTurbulenceId",
                table: "Images",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_RoadTurbulenceId",
                table: "Images",
                column: "RoadTurbulenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_RoadTurbulences_RoadTurbulenceId",
                table: "Images",
                column: "RoadTurbulenceId",
                principalTable: "RoadTurbulences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Hexagons_HexagonId",
                table: "RoadTurbulences",
                column: "HexagonId",
                principalTable: "Hexagons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
