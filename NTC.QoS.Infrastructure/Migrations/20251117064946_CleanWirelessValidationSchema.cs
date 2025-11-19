using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanWirelessValidationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF EXISTS to safely drop constraints and indexes
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    -- Drop foreign keys if they exist
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_WirelessValidations_Bands_BandId1') THEN
                        ALTER TABLE ""WirelessValidations"" DROP CONSTRAINT ""FK_WirelessValidations_Bands_BandId1"";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_WirelessValidations_ServiceProviders_ServiceProviderId1') THEN
                        ALTER TABLE ""WirelessValidations"" DROP CONSTRAINT ""FK_WirelessValidations_ServiceProviders_ServiceProviderId1"";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_WirelessValidations_Technologies_TechnologyId1') THEN
                        ALTER TABLE ""WirelessValidations"" DROP CONSTRAINT ""FK_WirelessValidations_Technologies_TechnologyId1"";
                    END IF;

                    -- Drop indexes if they exist
                    DROP INDEX IF EXISTS ""IX_WirelessValidations_BandId1"";
                    DROP INDEX IF EXISTS ""IX_WirelessValidations_ServiceProviderId1"";
                    DROP INDEX IF EXISTS ""IX_WirelessValidations_TechnologyId1"";

                    -- Drop columns if they exist
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WirelessValidations' AND column_name = 'BandId1') THEN
                        ALTER TABLE ""WirelessValidations"" DROP COLUMN ""BandId1"";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WirelessValidations' AND column_name = 'ServiceProviderId1') THEN
                        ALTER TABLE ""WirelessValidations"" DROP COLUMN ""ServiceProviderId1"";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WirelessValidations' AND column_name = 'TechnologyId1') THEN
                        ALTER TABLE ""WirelessValidations"" DROP COLUMN ""TechnologyId1"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BandId1",
                table: "WirelessValidations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProviderId1",
                table: "WirelessValidations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnologyId1",
                table: "WirelessValidations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidations_BandId1",
                table: "WirelessValidations",
                column: "BandId1");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidations_ServiceProviderId1",
                table: "WirelessValidations",
                column: "ServiceProviderId1");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidations_TechnologyId1",
                table: "WirelessValidations",
                column: "TechnologyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_WirelessValidations_Bands_BandId1",
                table: "WirelessValidations",
                column: "BandId1",
                principalTable: "Bands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WirelessValidations_ServiceProviders_ServiceProviderId1",
                table: "WirelessValidations",
                column: "ServiceProviderId1",
                principalTable: "ServiceProviders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WirelessValidations_Technologies_TechnologyId1",
                table: "WirelessValidations",
                column: "TechnologyId1",
                principalTable: "Technologies",
                principalColumn: "Id");
        }
    }
}
