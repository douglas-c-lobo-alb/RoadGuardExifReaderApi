using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hexagons_Images_ImageId",
                table: "Hexagons");

            migrationBuilder.DropIndex(
                name: "IX_Hexagons_ImageId",
                table: "Hexagons");

            migrationBuilder.DropColumn(
                name: "Anomaly",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Hexagons");

            migrationBuilder.AddColumn<string>(
                name: "AnomalyNotes",
                table: "Images",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Heading",
                table: "Images",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HexagonId",
                table: "Images",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Turbulence",
                table: "Images",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Hexagons",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "RoadVisualAnomalies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: false),
                    AnomalyType = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    BoxX1 = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxY1 = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxX2 = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxY2 = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadVisualAnomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadVisualAnomalies_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_HexagonId",
                table: "Images",
                column: "HexagonId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadVisualAnomalies_ImageId",
                table: "RoadVisualAnomalies",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Hexagons_HexagonId",
                table: "Images",
                column: "HexagonId",
                principalTable: "Hexagons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Hexagons_HexagonId",
                table: "Images");

            migrationBuilder.DropTable(
                name: "RoadVisualAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_Images_HexagonId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "AnomalyNotes",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Heading",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "HexagonId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Turbulence",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Hexagons");

            migrationBuilder.AddColumn<string>(
                name: "Anomaly",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "Hexagons",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hexagons_ImageId",
                table: "Hexagons",
                column: "ImageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Hexagons_Images_ImageId",
                table: "Hexagons",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id");
        }
    }
}
