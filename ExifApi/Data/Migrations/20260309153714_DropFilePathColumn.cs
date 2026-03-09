using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropFilePathColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Images",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
