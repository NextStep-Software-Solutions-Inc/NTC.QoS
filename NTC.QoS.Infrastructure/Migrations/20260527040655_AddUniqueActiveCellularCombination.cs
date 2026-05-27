using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveCellularCombination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "CellSectorId", "ServiceProviderId", "TechnologyId", "BandId"
                               ORDER BY "CreatedAt", "Id"
                           ) AS row_num
                    FROM "Cellulars"
                    WHERE "IsDeleted" = FALSE
                )
                UPDATE "Cellulars" AS c
                SET "IsDeleted" = TRUE,
                    "DeletedAt" = NOW(),
                    "DeletedBy" = 'migration-20260527040655',
                    "UpdatedAt" = NOW(),
                    "UpdatedBy" = 'migration-20260527040655'
                FROM ranked
                WHERE c."Id" = ranked."Id"
                  AND ranked.row_num > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band",
                table: "Cellulars",
                columns: new[] { "CellSectorId", "ServiceProviderId", "TechnologyId", "BandId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band",
                table: "Cellulars");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars",
                column: "CellSectorId");
        }
    }
}
