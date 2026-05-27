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
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                DROP INDEX CONCURRENTLY IF EXISTS "IX_Cellulars_CellSectorId";
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX CONCURRENTLY "IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band"
                ON "Cellulars" ("CellSectorId", "ServiceProviderId", "TechnologyId", "BandId")
                WHERE "IsDeleted" = FALSE;
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX CONCURRENTLY IF EXISTS "IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band";
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY "IX_Cellulars_CellSectorId"
                ON "Cellulars" ("CellSectorId");
                """, suppressTransaction: true);
        }
    }
}
