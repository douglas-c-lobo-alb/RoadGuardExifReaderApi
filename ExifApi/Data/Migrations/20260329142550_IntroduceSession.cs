using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Agents_AgentId",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Agents_AgentId",
                table: "RoadTurbulences");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Agents_AgentId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "Hexagons");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "Votes",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_AgentId",
                table: "Votes",
                newName: "IX_Votes_SessionId");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "RoadTurbulences",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_AgentId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_SessionId");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "Images",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Images_AgentId",
                table: "Images",
                newName: "IX_Images_SessionId");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "Agents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AgentId",
                table: "Sessions",
                column: "AgentId");

            // Old AgentId values are now in SessionId columns but point to Agents, not Sessions.
            // NULL them out so the new FK constraints can be satisfied.
            migrationBuilder.Sql("UPDATE \"Images\" SET \"SessionId\" = NULL;");
            migrationBuilder.Sql("UPDATE \"RoadTurbulences\" SET \"SessionId\" = NULL;");
            migrationBuilder.Sql("UPDATE \"Votes\" SET \"SessionId\" = NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Sessions_SessionId",
                table: "Images",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Sessions_SessionId",
                table: "RoadTurbulences",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Sessions_SessionId",
                table: "Votes",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Sessions_SessionId",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadTurbulences_Sessions_SessionId",
                table: "RoadTurbulences");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Sessions_SessionId",
                table: "Votes");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Votes",
                newName: "AgentId");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_SessionId",
                table: "Votes",
                newName: "IX_Votes_AgentId");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "RoadTurbulences",
                newName: "AgentId");

            migrationBuilder.RenameIndex(
                name: "IX_RoadTurbulences_SessionId",
                table: "RoadTurbulences",
                newName: "IX_RoadTurbulences_AgentId");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Images",
                newName: "AgentId");

            migrationBuilder.RenameIndex(
                name: "IX_Images_SessionId",
                table: "Images",
                newName: "IX_Images_AgentId");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "Hexagons",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Agents_AgentId",
                table: "Images",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadTurbulences_Agents_AgentId",
                table: "RoadTurbulences",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Agents_AgentId",
                table: "Votes",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
