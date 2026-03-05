using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Latitude = table.Column<decimal>(type: "TEXT", precision: 10, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", precision: 10, scale: 6, nullable: true),
                    Altitude = table.Column<double>(type: "REAL", nullable: true),
                    CameraMake = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CameraModel = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DateTaken = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Anomaly = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hexagons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    H3Index = table.Column<string>(type: "TEXT", nullable: false),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hexagons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hexagons_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hexagons_ImageId",
                table: "Hexagons",
                column: "ImageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hexagons");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
