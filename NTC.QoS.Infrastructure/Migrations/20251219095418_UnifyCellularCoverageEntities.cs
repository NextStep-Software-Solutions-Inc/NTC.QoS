using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyCellularCoverageEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Safely rename Strength to SignalStrength if it exists
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'CellularCoverages' 
                        AND column_name = 'Strength'
                    ) THEN
                        ALTER TABLE ""CellularCoverages"" RENAME COLUMN ""Strength"" TO ""SignalStrength"";
                    END IF;
                END $$;
            ");

            // Step 2: Data migration note
            // WARNING: CellularCoveragesV2 will be dropped without copying data
            // If you need to preserve V2 data, run this SQL manually BEFORE applying this migration:
            //
            // INSERT INTO "CellularCoverages" ("Id", "NetworkId", "ServiceProviderId", "BandId", "TechnologyId", 
            //   "CellSectorId", "MunicipalityId", "Location", "SignalStrength", "Address", "IsFileUploaded",
            //   "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy")
            // SELECT "Id", "NetworkId", NULL, "BandId", "TechnologyId", "CellSectorId", "MunicipalityId", 
            //   "Location", "SignalStrength", "Address", "IsFileUploaded", "CreatedAt", "UpdatedAt", 
            //   "CreatedBy", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy"
            // FROM "CellularCoveragesV2"
            // WHERE "Id" NOT IN (SELECT "Id" FROM "CellularCoverages");

            // Step 3: Drop the CellularCoveragesV2 table
            migrationBuilder.DropTable(
                name: "CellularCoveragesV2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_Bands_BandId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_CellSectors_CellSectorId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_Municipalities_MunicipalityId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_ServiceProviders_ServiceProviderId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_Technologies_TechnologyId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_BandId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_CellSectorId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_MunicipalityId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_ServiceProviderId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_TechnologyId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "BandId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "CellSectorId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "TechnologyId",
                table: "CellularCoverages");

            migrationBuilder.RenameColumn(
                name: "SignalStrength",
                table: "CellularCoverages",
                newName: "Strength");

            migrationBuilder.CreateTable(
                name: "CellularCoveragesV2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CellSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    SignalStrength = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellularCoveragesV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                        column: x => x.CellSectorId,
                        principalTable: "CellSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_BandId",
                table: "CellularCoveragesV2",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_TechnologyId",
                table: "CellularCoveragesV2",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageV2_CellSectorId",
                table: "CellularCoveragesV2",
                column: "CellSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageV2_Location",
                table: "CellularCoveragesV2",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageV2_MunicipalityId",
                table: "CellularCoveragesV2",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageV2_Network_Band_Municipality",
                table: "CellularCoveragesV2",
                columns: new[] { "NetworkId", "BandId", "MunicipalityId" });
        }
    }
}
