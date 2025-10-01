using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedConnectivityAssessmentAndWifiHotspotEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectivityAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationType = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geography (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    Boundary = table.Column<MultiPolygon>(type: "geography (MultiPolygon,4326)", nullable: true),
                    AreaSizeKm2 = table.Column<double>(type: "double precision", nullable: false),
                    Population = table.Column<int>(type: "integer", nullable: true),
                    HouseholdCount = table.Column<int>(type: "integer", nullable: true),
                    AssessmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConnectivityStatus = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    FiberCoverageScore = table.Column<int>(type: "integer", nullable: false),
                    InternetAccessScore = table.Column<int>(type: "integer", nullable: false),
                    TechnologyTypeScore = table.Column<int>(type: "integer", nullable: false),
                    QualityOfServiceScore = table.Column<int>(type: "integer", nullable: false),
                    CoverageAvailabilityScore = table.Column<int>(type: "integer", nullable: false),
                    CellTowerCount = table.Column<int>(type: "integer", nullable: false),
                    FiberNAPCount = table.Column<int>(type: "integer", nullable: false),
                    WifiHotspotCount = table.Column<int>(type: "integer", nullable: false),
                    AverageDownloadSpeedMbps = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageUploadSpeedMbps = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageLatencyMs = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianDownloadSpeedMbps = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianUploadSpeedMbps = table.Column<decimal>(type: "numeric", nullable: true),
                    SpeedTestCount = table.Column<int>(type: "integer", nullable: false),
                    ServiceProviderCount = table.Column<int>(type: "integer", nullable: false),
                    DominantServiceProvider = table.Column<string>(type: "text", nullable: true),
                    MobileCoveragePercentage = table.Column<double>(type: "double precision", nullable: true),
                    FixedBroadbandCoveragePercentage = table.Column<double>(type: "double precision", nullable: true),
                    FiberCoveragePercentage = table.Column<double>(type: "double precision", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    HasReliableConnectivity = table.Column<bool>(type: "boolean", nullable: false),
                    MeetsMinimumSpeedRequirement = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssessedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectivityAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WiFiHotspots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geography (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    CoverageRadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxBandwidthMbps = table.Column<decimal>(type: "numeric", nullable: true),
                    FrequencyBand = table.Column<string>(type: "text", nullable: true),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    InstallationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstablishmentType = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiFiHotspots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WiFiHotspots_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Boundary",
                table: "ConnectivityAssessments",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Location",
                table: "ConnectivityAssessments",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Location_Type",
                table: "ConnectivityAssessments",
                columns: new[] { "LocationId", "LocationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Status",
                table: "ConnectivityAssessments",
                column: "ConnectivityStatus");

            migrationBuilder.CreateIndex(
                name: "IX_WiFiHotspot_Active_Public",
                table: "WiFiHotspots",
                columns: new[] { "IsActive", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_WiFiHotspot_Location",
                table: "WiFiHotspots",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_WiFiHotspots_ServiceProviderId",
                table: "WiFiHotspots",
                column: "ServiceProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectivityAssessments");

            migrationBuilder.DropTable(
                name: "WiFiHotspots");
        }
    }
}
