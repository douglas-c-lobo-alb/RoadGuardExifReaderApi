using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExifApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueH3Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicate existing Hexagons rows before enforcing uniqueness.
            // For each duplicate H3Index group, keep the row with the lowest Id
            // and re-point all FK references to it, then delete the extras.
            migrationBuilder.Sql("""
                UPDATE "Images"
                SET "HexagonId" = (
                    SELECT MIN(h2."Id") FROM "Hexagons" h2
                    WHERE h2."H3Index" = (SELECT h1."H3Index" FROM "Hexagons" h1 WHERE h1."Id" = "Images"."HexagonId")
                )
                WHERE "HexagonId" IN (
                    SELECT "Id" FROM "Hexagons"
                    WHERE "H3Index" IN (SELECT "H3Index" FROM "Hexagons" GROUP BY "H3Index" HAVING COUNT(*) > 1)
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "RoadVisualAnomalies"
                SET "HexagonId" = (
                    SELECT MIN(h2."Id") FROM "Hexagons" h2
                    WHERE h2."H3Index" = (SELECT h1."H3Index" FROM "Hexagons" h1 WHERE h1."Id" = "RoadVisualAnomalies"."HexagonId")
                )
                WHERE "HexagonId" IN (
                    SELECT "Id" FROM "Hexagons"
                    WHERE "H3Index" IN (SELECT "H3Index" FROM "Hexagons" GROUP BY "H3Index" HAVING COUNT(*) > 1)
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "RoadTurbulences"
                SET "HexagonId" = (
                    SELECT MIN(h2."Id") FROM "Hexagons" h2
                    WHERE h2."H3Index" = (SELECT h1."H3Index" FROM "Hexagons" h1 WHERE h1."Id" = "RoadTurbulences"."HexagonId")
                )
                WHERE "HexagonId" IN (
                    SELECT "Id" FROM "Hexagons"
                    WHERE "H3Index" IN (SELECT "H3Index" FROM "Hexagons" GROUP BY "H3Index" HAVING COUNT(*) > 1)
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "Votes"
                SET "HexagonId" = (
                    SELECT MIN(h2."Id") FROM "Hexagons" h2
                    WHERE h2."H3Index" = (SELECT h1."H3Index" FROM "Hexagons" h1 WHERE h1."Id" = "Votes"."HexagonId")
                )
                WHERE "HexagonId" IN (
                    SELECT "Id" FROM "Hexagons"
                    WHERE "H3Index" IN (SELECT "H3Index" FROM "Hexagons" GROUP BY "H3Index" HAVING COUNT(*) > 1)
                );
                """);

            // Delete the now-orphaned duplicate rows (keep lowest Id per H3Index)
            migrationBuilder.Sql("""
                DELETE FROM "Hexagons"
                WHERE "Id" NOT IN (SELECT MIN("Id") FROM "Hexagons" GROUP BY "H3Index");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Hexagons_H3Index",
                table: "Hexagons",
                column: "H3Index",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hexagons_H3Index",
                table: "Hexagons");
        }
    }
}
