using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeCellularCoverageV2IntoV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Copy all data from CellularCoveragesV2 to CellularCoverages
            migrationBuilder.Sql(@"
                INSERT INTO ""CellularCoverages"" 
                (
                    ""Id"", 
                    ""NetworkId"", 
                    ""ServiceProviderId"",
                    ""BandId"", 
                    ""TechnologyId"", 
                    ""CellSectorId"", 
                    ""MunicipalityId"", 
                    ""Location"", 
                    ""SignalStrength"", 
                    ""Address"", 
                    ""IsFileUploaded"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""CreatedBy"",
                    ""UpdatedBy"",
                    ""IsDeleted""
                )
                SELECT 
                    ""Id"",
                    ""NetworkId"",
                    NULL as ""ServiceProviderId"",  -- V2 doesn't have ServiceProvider
                    ""BandId"",
                    ""TechnologyId"",
                    ""CellSectorId"",
                    ""MunicipalityId"",
                    ""Location"",
                    ""SignalStrength"",
                    ""Address"",
                    ""IsFileUploaded"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""CreatedBy"",
                    ""UpdatedBy"",
                    ""IsDeleted""
                FROM ""CellularCoveragesV2""
                WHERE ""Id"" NOT IN (SELECT ""Id"" FROM ""CellularCoverages"");
            ");

            // Step 2: Drop foreign key constraints on CellularCoveragesV2
            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Bands_BandId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Networks_NetworkId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                table: "CellularCoveragesV2");

            // Step 3: Drop indexes on CellularCoveragesV2
            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_BandId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_CellSectorId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_Location",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_MunicipalityId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_NetworkId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_TechnologyId",
                table: "CellularCoveragesV2");

            // Step 4: Drop the CellularCoveragesV2 table
            migrationBuilder.DropTable(
                name: "CellularCoveragesV2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate CellularCoveragesV2 table
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
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<NetTopologySuite.Geometries.Point>(type: "geometry(Point,4326)", nullable: false),
                    SignalStrength = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_BandId",
                table: "CellularCoveragesV2",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_CellSectorId",
                table: "CellularCoveragesV2",
                column: "CellSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_Location",
                table: "CellularCoveragesV2",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_MunicipalityId",
                table: "CellularCoveragesV2",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_NetworkId",
                table: "CellularCoveragesV2",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_TechnologyId",
                table: "CellularCoveragesV2",
                column: "TechnologyId");

            // Copy data back (only records that came from V2)
            migrationBuilder.Sql(@"
                INSERT INTO ""CellularCoveragesV2"" 
                (
                    ""Id"", 
                    ""NetworkId"", 
                    ""BandId"", 
                    ""TechnologyId"", 
                    ""CellSectorId"", 
                    ""MunicipalityId"", 
                    ""Location"", 
                    ""SignalStrength"", 
                    ""Address"", 
                    ""IsFileUploaded"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""CreatedBy"",
                    ""UpdatedBy"",
                    ""IsDeleted""
                )
                SELECT 
                    ""Id"",
                    ""NetworkId"",
                    ""BandId"",
                    ""TechnologyId"",
                    ""CellSectorId"",
                    ""MunicipalityId"",
                    ""Location"",
                    ""SignalStrength"",
                    ""Address"",
                    ""IsFileUploaded"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""CreatedBy"",
                    ""UpdatedBy"",
                    ""IsDeleted""
                FROM ""CellularCoverages""
                WHERE ""ServiceProviderId"" IS NULL
                AND ""BandId"" IS NOT NULL
                AND ""TechnologyId"" IS NOT NULL
                AND ""CellSectorId"" IS NOT NULL
                AND ""MunicipalityId"" IS NOT NULL;
            ");
        }
    }
}
