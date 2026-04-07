using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorkingOnAddingRoadTurbulencesForTheImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Turbulence",
                table: "Images",
                newName: "RoadTurbulenceId");

            migrationBuilder.AlterColumn<int>(
                name: "ImageId",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_RoadTurbulences_RoadTurbulenceId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_RoadTurbulenceId",
                table: "Images");

            migrationBuilder.RenameColumn(
                name: "RoadTurbulenceId",
                table: "Images",
                newName: "Turbulence");

            migrationBuilder.AlterColumn<int>(
                name: "ImageId",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
