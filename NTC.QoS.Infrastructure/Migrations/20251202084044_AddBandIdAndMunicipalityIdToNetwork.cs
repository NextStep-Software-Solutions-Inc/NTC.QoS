using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBandIdAndMunicipalityIdToNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // STEP 1: Add BandId column as nullable first to allow existing data to remain
            migrationBuilder.AddColumn<Guid>(
                name: "BandId",
                table: "Networks",
                type: "uuid",
                nullable: true);

            // STEP 2: Add MunicipalityId column (already nullable)
            migrationBuilder.AddColumn<Guid>(
                name: "MunicipalityId",
                table: "Networks",
                type: "uuid",
                nullable: true);

            // STEP 3: Data migration - Populate BandId from SystemType relationship if possible
            // This attempts to derive the Band from the SystemType's Generation
            // Networks without a valid SystemType->Generation->Band relationship will need manual intervention
            migrationBuilder.Sql(@"
                UPDATE ""Networks"" n
                SET ""BandId"" = (
                    SELECT gb.""BandId""
                    FROM ""SystemTypes"" st
                    INNER JOIN ""Generations"" g ON st.""GenerationId"" = g.""Id""
                    INNER JOIN ""GenerationBands"" gb ON g.""Id"" = gb.""GenerationId""
                    WHERE st.""Id"" = n.""SystemTypeId""
                    AND gb.""IsDeleted"" = false
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM ""SystemTypes"" st
                    INNER JOIN ""Generations"" g ON st.""GenerationId"" = g.""Id""
                    INNER JOIN ""GenerationBands"" gb ON g.""Id"" = gb.""GenerationId""
                    WHERE st.""Id"" = n.""SystemTypeId""
                    AND gb.""IsDeleted"" = false
                );
            ");

            // STEP 4: Pre-migration validation - Check if any Networks still have NULL BandId
            // This will fail the migration if data cannot be migrated automatically
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    null_band_count INTEGER;
                BEGIN
                    SELECT COUNT(*) INTO null_band_count
                    FROM ""Networks""
                    WHERE ""BandId"" IS NULL AND ""IsDeleted"" = false;
                    
                    IF null_band_count > 0 THEN
                        RAISE EXCEPTION 'Cannot complete migration: % Networks have NULL BandId. Please manually assign BandId values to these Networks before retrying.', null_band_count;
                    END IF;
                END $$;
            ");

            // STEP 5: Now that BandId is populated, make it required (NOT NULL)
            migrationBuilder.AlterColumn<Guid>(
                name: "BandId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // STEP 6: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Networks_BandId",
                table: "Networks",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_MunicipalityId",
                table: "Networks",
                column: "MunicipalityId");

            // STEP 7: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Bands_BandId",
                table: "Networks",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Municipalities_MunicipalityId",
                table: "Networks",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Bands_BandId",
                table: "Networks");

            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Municipalities_MunicipalityId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_BandId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_MunicipalityId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "BandId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Networks");
        }
    }
}
