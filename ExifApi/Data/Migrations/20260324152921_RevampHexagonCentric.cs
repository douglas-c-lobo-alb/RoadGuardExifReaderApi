using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RevampHexagonCentric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Images_ImageId",
                table: "RoadTurbulences");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadVisualAnomalies_Images_ImageId",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropColumn(
                name: "AlreadyVoted",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropColumn(
                name: "AnomalyType",
                table: "RoadVisualAnomalies");

            migrationBuilder.RenameColumn(
                name: "UpVote",
                table: "RoadVisualAnomalies",
                newName: "Kind");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "RoadVisualAnomalies",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "DownVote",
                table: "RoadVisualAnomalies",
                newName: "HexagonId");

            migrationBuilder.RenameColumn(
                name: "RoadTurbulenceType",
                table: "RoadTurbulences",
                newName: "Kind");

            migrationBuilder.RenameColumn(
                name: "ImageId",
                table: "RoadTurbulences",
                newName: "AgentId");

            migrationBuilder.RenameColumn(
                name: "DateCreated",
                table: "RoadTurbulences",
                newName: "LastModifiedDate");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_ImageId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_AgentId");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Images",
                newName: "Metadata");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "RoadTurbulences",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "HexagonId",
                table: "RoadTurbulences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "RoadTurbulences",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "Hexagons",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HexagonId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<decimal>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    BoxX1 = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxY1 = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxX2 = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxY2 = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Votes_Hexagons_HexagonId",
                        column: x => x.HexagonId,
                        principalTable: "Hexagons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votes_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadVisualAnomalies_HexagonId",
                table: "RoadVisualAnomalies",
                column: "HexagonId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadTurbulences_HexagonId",
                table: "RoadTurbulences",
                column: "HexagonId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_AgentId",
                table: "Votes",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_HexagonId",
                table: "Votes",
                column: "HexagonId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ImageId",
                table: "Votes",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Agents_AgentId",
                table: "RoadTurbulences",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Hexagons_HexagonId",
                table: "RoadTurbulences",
                column: "HexagonId",
                principalTable: "Hexagons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadVisualAnomalies_Hexagons_HexagonId",
                table: "RoadVisualAnomalies",
                column: "HexagonId",
                principalTable: "Hexagons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadVisualAnomalies_Images_ImageId",
                table: "RoadVisualAnomalies",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Agents_AgentId",
                table: "RoadTurbulences");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Hexagons_HexagonId",
                table: "RoadTurbulences");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadVisualAnomalies_Hexagons_HexagonId",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadVisualAnomalies_Images_ImageId",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_RoadVisualAnomalies_HexagonId",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_RoadTurbulences_HexagonId",
                table: "RoadTurbulences");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "RoadTurbulences");

            migrationBuilder.DropColumn(
                name: "HexagonId",
                table: "RoadTurbulences");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "RoadTurbulences");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "Hexagons");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "RoadVisualAnomalies",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Kind",
                table: "RoadVisualAnomalies",
                newName: "UpVote");

            migrationBuilder.RenameColumn(
                name: "HexagonId",
                table: "RoadVisualAnomalies",
                newName: "DownVote");

            migrationBuilder.RenameColumn(
                name: "LastModifiedDate",
                table: "RoadTurbulences",
                newName: "DateCreated");

            migrationBuilder.RenameColumn(
                name: "Kind",
                table: "RoadTurbulences",
                newName: "RoadTurbulenceType");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "RoadTurbulences",
                newName: "ImageId");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_AgentId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_ImageId");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Images",
                newName: "Notes");

            migrationBuilder.AddColumn<bool>(
                name: "AlreadyVoted",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AnomalyType",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Images_ImageId",
                table: "RoadTurbulences",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadVisualAnomalies_Images_ImageId",
                table: "RoadVisualAnomalies",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
