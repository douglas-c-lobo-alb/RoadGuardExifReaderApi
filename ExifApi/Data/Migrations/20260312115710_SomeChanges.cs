using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class SomeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnomalyNotes",
                table: "Images",
                newName: "Notes");

            migrationBuilder.AddColumn<bool>(
                name: "AlreadyVoted",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DownVote",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpVote",
                table: "RoadVisualAnomalies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlreadyVoted",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropColumn(
                name: "DownVote",
                table: "RoadVisualAnomalies");

            migrationBuilder.DropColumn(
                name: "UpVote",
                table: "RoadVisualAnomalies");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Images",
                newName: "AnomalyNotes");
        }
    }
}
