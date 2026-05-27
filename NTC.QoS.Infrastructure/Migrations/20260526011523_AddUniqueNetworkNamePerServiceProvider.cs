using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueNetworkNamePerServiceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Networks"
                SET "Name" = lower(btrim("Name"))
                WHERE "IsDeleted" = FALSE
                  AND "Name" <> lower(btrim("Name"));
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS network_dedup_map;
                CREATE TEMP TABLE network_dedup_map AS
                WITH ranked AS (
                    SELECT "Id",
                           "ServiceProviderId",
                           "Name",
                           ROW_NUMBER() OVER (
                               PARTITION BY "ServiceProviderId", "Name"
                               ORDER BY "CreatedAt", "Id"
                           ) AS row_num
                    FROM "Networks"
                    WHERE "IsDeleted" = FALSE
                )
                SELECT loser."Id" AS loser_id,
                       winner."Id" AS winner_id
                FROM ranked loser
                INNER JOIN ranked winner
                    ON winner."ServiceProviderId" = loser."ServiceProviderId"
                   AND winner."Name" = loser."Name"
                   AND winner.row_num = 1
                WHERE loser.row_num > 1;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                UPDATE "Cellulars" AS c
                SET "NetworkId" = m.winner_id
                FROM network_dedup_map AS m
                WHERE c."NetworkId" = m.loser_id;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                UPDATE "Sectors" AS s
                SET "NetworkId" = m.winner_id
                FROM network_dedup_map AS m
                WHERE s."NetworkId" = m.loser_id;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                UPDATE "CellularCoverages" AS cc
                SET "NetworkId" = m.winner_id
                FROM network_dedup_map AS m
                WHERE cc."NetworkId" = m.loser_id;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                UPDATE "StudyNetworks" AS sn
                SET "NetworkId" = m.winner_id
                FROM network_dedup_map AS m
                WHERE sn."NetworkId" = m.loser_id;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "ServiceProviderId", "Name"
                               ORDER BY "CreatedAt", "Id"
                           ) AS row_num
                    FROM "Networks"
                    WHERE "IsDeleted" = FALSE
                )
                UPDATE "Networks" AS n
                SET "IsDeleted" = TRUE,
                    "DeletedAt" = NOW(),
                    "DeletedBy" = 'migration-20260526011523'
                FROM ranked
                WHERE n."Id" = ranked."Id"
                  AND ranked.row_num > 1;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS network_dedup_map;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                DROP INDEX CONCURRENTLY IF EXISTS "IX_Networks_ServiceProviderId";
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX CONCURRENTLY "IX_Networks_ServiceProviderId_NormalizedName"
                ON "Networks" ("ServiceProviderId", "Name")
                WHERE "IsDeleted" = FALSE;
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX CONCURRENTLY IF EXISTS "IX_Networks_ServiceProviderId_NormalizedName";
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY "IX_Networks_ServiceProviderId"
                ON "Networks" ("ServiceProviderId");
                """, suppressTransaction: true);
        }
    }
}
