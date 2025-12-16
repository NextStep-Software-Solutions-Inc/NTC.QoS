using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "AntennaPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    URL = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntennaPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AreaStudies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaStudies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectivityAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationType = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
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
                    AverageSignalStrength = table.Column<double>(type: "double precision", nullable: true),
                    ServiceProviderCount = table.Column<int>(type: "integer", nullable: false),
                    DominantServiceProvider = table.Column<string>(type: "text", nullable: true),
                    MobileCoveragePercentage = table.Column<double>(type: "double precision", nullable: true),
                    FixedBroadbandCoveragePercentage = table.Column<double>(type: "double precision", nullable: true),
                    FiberCoveragePercentage = table.Column<double>(type: "double precision", nullable: true),
                    EstimatedPopulationCoveragePercent = table.Column<double>(type: "double precision", nullable: true),
                    EstimatedHouseholdCoveragePercent = table.Column<double>(type: "double precision", nullable: true),
                    AssetsPerThousandPeople = table.Column<double>(type: "double precision", nullable: true),
                    ProvidersPerThousandPeople = table.Column<double>(type: "double precision", nullable: true),
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
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectivityAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Generations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MIMOs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    DLCoverageGain = table.Column<decimal>(type: "numeric", nullable: false),
                    ULCoverageGain = table.Column<decimal>(type: "numeric", nullable: false),
                    DLThroughput = table.Column<decimal>(type: "numeric", nullable: false),
                    ULThroughput = table.Column<decimal>(type: "numeric", nullable: false),
                    DLInterferenceReduction = table.Column<decimal>(type: "numeric", nullable: false),
                    ULInterferenceReduction = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MIMOs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    URL = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LicenseNumber = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: false),
                    LogoData = table.Column<byte[]>(type: "bytea", nullable: true),
                    LogoContentType = table.Column<string>(type: "text", nullable: true),
                    LogoFileName = table.Column<string>(type: "text", nullable: true),
                    LogoUploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Boundaries = table.Column<MultiPolygon>(type: "geometry (MultiPolygon,4326)", nullable: true, defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY")),
                    Center = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Area = table.Column<float>(type: "real", nullable: false),
                    TotalPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Studies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaStudyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StudyRadius = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Studies_AreaStudies_AreaStudyId",
                        column: x => x.AreaStudyId,
                        principalTable: "AreaStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectivityAssessmentProvider",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectivityAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    CellTowerCount = table.Column<int>(type: "integer", nullable: false),
                    FiberNAPCount = table.Column<int>(type: "integer", nullable: false),
                    WiFiHotspotCount = table.Column<int>(type: "integer", nullable: false),
                    WirelessValidationCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAssetCount = table.Column<int>(type: "integer", nullable: false),
                    AssetPercentage = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectivityAssessmentProvider", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectivityAssessmentProvider_ConnectivityAssessments_Conn~",
                        column: x => x.ConnectivityAssessmentId,
                        principalTable: "ConnectivityAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenerationBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationBands_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GenerationBands_Generations_GenerationId",
                        column: x => x.GenerationId,
                        principalTable: "Generations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Standard = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Technologies_Generations_GenerationId",
                        column: x => x.GenerationId,
                        principalTable: "Generations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Boundaries = table.Column<MultiPolygon>(type: "geometry (MultiPolygon,4326)", nullable: true, defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY")),
                    Center = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Area = table.Column<float>(type: "real", nullable: false),
                    TotalPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Provinces_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MNC = table.Column<string>(type: "text", nullable: false),
                    MCC = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthUserId = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    UserTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_UserTypes_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "UserTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemTypes_Generations_GenerationId",
                        column: x => x.GenerationId,
                        principalTable: "Generations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemTypes_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyBands_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologyBands_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCity = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Boundaries = table.Column<MultiPolygon>(type: "geometry (MultiPolygon,4326)", nullable: true, defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY")),
                    Center = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Area = table.Column<float>(type: "real", nullable: false),
                    TotalPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Municipalities_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Group = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CID = table.Column<string>(type: "text", nullable: false),
                    SiteId = table.Column<string>(type: "text", nullable: false),
                    TowerType = table.Column<string>(type: "text", nullable: false),
                    Elevation = table.Column<float>(type: "real", nullable: false),
                    TAC = table.Column<string>(type: "text", nullable: false),
                    LAC = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cells_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cells_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CenterFrequency = table.Column<decimal>(type: "numeric", nullable: false),
                    LowerFrequency = table.Column<decimal>(type: "numeric", nullable: false),
                    UpperFrequency = table.Column<decimal>(type: "numeric", nullable: false),
                    RxThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    IsFromUploadedFile = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Links_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Links_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NAPs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NAPs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NAPs_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NAPs_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WiFiHotspots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
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
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "UserRegions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRegions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRegions_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRegions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SystemTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ULCenterFreq = table.Column<decimal>(type: "numeric", nullable: false),
                    DLCenterFreq = table.Column<decimal>(type: "numeric", nullable: false),
                    Bandwidth = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Networks_NetworkTemplates_NetworkTemplateId",
                        column: x => x.NetworkTemplateId,
                        principalTable: "NetworkTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Networks_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Networks_SystemTypes_SystemTypeId",
                        column: x => x.SystemTypeId,
                        principalTable: "SystemTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Barangays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Boundaries = table.Column<MultiPolygon>(type: "geometry (MultiPolygon,4326)", nullable: true, defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY")),
                    Center = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Area = table.Column<float>(type: "real", nullable: false),
                    TotalPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdPopulation = table.Column<int>(type: "integer", nullable: true),
                    HouseholdCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barangays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Barangays_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyMunicipalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyMunicipalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyMunicipalities_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyMunicipalities_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CellSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CellId = table.Column<Guid>(type: "uuid", nullable: false),
                    AntennaType = table.Column<int>(type: "integer", nullable: false),
                    AntennaHeight = table.Column<decimal>(type: "numeric", nullable: false),
                    Azimuth = table.Column<decimal>(type: "numeric", nullable: false),
                    BeamTilt = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellSectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellSectors_Cells_CellId",
                        column: x => x.CellId,
                        principalTable: "Cells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CellularCoverages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Strength = table.Column<decimal>(type: "numeric", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellularCoverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellularCoverages_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyNetworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyNetworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyNetworks_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyNetworks_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Speedtests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ping = table.Column<decimal>(type: "numeric", nullable: false),
                    Jitter = table.Column<decimal>(type: "numeric", nullable: false),
                    RSSI = table.Column<decimal>(type: "numeric", nullable: true),
                    UploadSpeed = table.Column<decimal>(type: "numeric", nullable: false),
                    DownloadSpeed = table.Column<decimal>(type: "numeric", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point,4326)", nullable: false, defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)")),
                    PhoneModel = table.Column<string>(type: "text", nullable: false),
                    MCC = table.Column<string>(type: "text", nullable: false),
                    MNC = table.Column<string>(type: "text", nullable: false),
                    TAC = table.Column<string>(type: "text", nullable: false),
                    SignalQuality = table.Column<string>(type: "text", nullable: true),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    CellIdentifier = table.Column<string>(type: "text", nullable: false),
                    IsFixedBroadband = table.Column<bool>(type: "boolean", nullable: false),
                    BarangayId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Speedtests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Speedtests_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Speedtests_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Speedtests_Generations_GenerationId",
                        column: x => x.GenerationId,
                        principalTable: "Generations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Speedtests_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Speedtests_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WirelessValidations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BarangayId = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    RSRP = table.Column<decimal>(type: "numeric", nullable: true),
                    IsMarginal = table.Column<bool>(type: "boolean", nullable: false),
                    BandId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceProviderId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnologyId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WirelessValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Bands_BandId1",
                        column: x => x.BandId1,
                        principalTable: "Bands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_ServiceProviders_ServiceProviderId1",
                        column: x => x.ServiceProviderId1,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Technologies_TechnologyId1",
                        column: x => x.TechnologyId1,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Zone",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarangayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zone_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cellulars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CellSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    DLCenterFreq = table.Column<decimal>(type: "numeric", nullable: false),
                    ULCenterFreq = table.Column<decimal>(type: "numeric", nullable: false),
                    Bandwidth = table.Column<decimal>(type: "numeric", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cellulars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cellulars_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cellulars_CellSectors_CellSectorId",
                        column: x => x.CellSectorId,
                        principalTable: "CellSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cellulars_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cellulars_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cellulars_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cellulars_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: true),
                    MIMOId = table.Column<Guid>(type: "uuid", nullable: true),
                    CellSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TxAntennaPatternId = table.Column<Guid>(type: "uuid", nullable: true),
                    RxAntennaPatternId = table.Column<Guid>(type: "uuid", nullable: true),
                    AntennaType = table.Column<int>(type: "integer", nullable: true),
                    RadioEquipment = table.Column<string>(type: "text", nullable: true),
                    IsRxEqualTx = table.Column<bool>(type: "boolean", nullable: true),
                    TxPower = table.Column<decimal>(type: "numeric", nullable: true),
                    TxAntennaModel = table.Column<string>(type: "text", nullable: true),
                    TxCableType = table.Column<string>(type: "text", nullable: true),
                    TxCableLength = table.Column<decimal>(type: "numeric", nullable: true),
                    TxCableLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    TxAdditionalLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    TxAntennaHeight = table.Column<decimal>(type: "numeric", nullable: true),
                    TxAntennaGain = table.Column<decimal>(type: "numeric", nullable: true),
                    TxAzimuth = table.Column<decimal>(type: "numeric", nullable: true),
                    TxBeamTilt = table.Column<decimal>(type: "numeric", nullable: true),
                    TxEIRP = table.Column<decimal>(type: "numeric", nullable: true),
                    RxAntennaModel = table.Column<string>(type: "text", nullable: true),
                    RxCableType = table.Column<string>(type: "text", nullable: true),
                    RxCableLength = table.Column<decimal>(type: "numeric", nullable: true),
                    RxCableLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    RxAdditionalLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    RxAntennaHeight = table.Column<decimal>(type: "numeric", nullable: true),
                    RxAntennaGain = table.Column<decimal>(type: "numeric", nullable: true),
                    RxAzimuth = table.Column<decimal>(type: "numeric", nullable: true),
                    RxBeamTilt = table.Column<decimal>(type: "numeric", nullable: true),
                    RxEIRP = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sectors_AntennaPatterns_RxAntennaPatternId",
                        column: x => x.RxAntennaPatternId,
                        principalTable: "AntennaPatterns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sectors_AntennaPatterns_TxAntennaPatternId",
                        column: x => x.TxAntennaPatternId,
                        principalTable: "AntennaPatterns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sectors_CellSectors_CellSectorId",
                        column: x => x.CellSectorId,
                        principalTable: "CellSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sectors_MIMOs_MIMOId",
                        column: x => x.MIMOId,
                        principalTable: "MIMOs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sectors_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaStudies_Name",
                table: "AreaStudies",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Bands_Name",
                table: "Bands",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Barangay_Boundaries",
                table: "Barangays",
                column: "Boundaries")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Barangay_Center",
                table: "Barangays",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Barangays_MunicipalityId",
                table: "Barangays",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Cell_Location",
                table: "Cells",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Cells_FileId",
                table: "Cells",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Cells_Name",
                table: "Cells",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Cells_ServiceProviderId",
                table: "Cells",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_CellSectors_CellId",
                table: "CellSectors",
                column: "CellId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverage_Location",
                table: "CellularCoverages",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_NetworkId",
                table: "CellularCoverages",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_BandId",
                table: "Cellulars",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars",
                column: "CellSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_FileId",
                table: "Cellulars",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_NetworkId",
                table: "Cellulars",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_ServiceProviderId",
                table: "Cellulars",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_TechnologyId",
                table: "Cellulars",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessmentProvider_ConnectivityAssessmentId",
                table: "ConnectivityAssessmentProvider",
                column: "ConnectivityAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Location",
                table: "ConnectivityAssessments",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_LocationId",
                table: "ConnectivityAssessments",
                column: "LocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_LocationType",
                table: "ConnectivityAssessments",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Status",
                table: "ConnectivityAssessments",
                column: "ConnectivityStatus");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationBands_BandId",
                table: "GenerationBands",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationBands_GenerationId",
                table: "GenerationBands",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_BandId",
                table: "Links",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_ServiceProviderId",
                table: "Links",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_ProvinceId",
                table: "Municipalities",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipality_Boundaries",
                table: "Municipalities",
                column: "Boundaries")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Municipality_Center",
                table: "Municipalities",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_NAP_Location",
                table: "NAPs",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_NAPs_FileId",
                table: "NAPs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_NAPs_ServiceProviderId",
                table: "NAPs",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_NetworkTemplateId",
                table: "Networks",
                column: "NetworkTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_ServiceProviderId",
                table: "Networks",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_SystemTypeId",
                table: "Networks",
                column: "SystemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Name",
                table: "Providers",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Province_Boundaries",
                table: "Provinces",
                column: "Boundaries")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Province_Center",
                table: "Provinces",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_RegionId",
                table: "Provinces",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Region_Boundaries",
                table: "Regions",
                column: "Boundaries")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Region_Center",
                table: "Regions",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId_RoleId_Active",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_CellSectorId",
                table: "Sectors",
                column: "CellSectorId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_MIMOId",
                table: "Sectors",
                column: "MIMOId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_NetworkId",
                table: "Sectors",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_RxAntennaPatternId",
                table: "Sectors",
                column: "RxAntennaPatternId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_TxAntennaPatternId",
                table: "Sectors",
                column: "TxAntennaPatternId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ProviderId",
                table: "ServiceProviders",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders",
                columns: new[] { "ServiceId", "ProviderId", "MNC" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name_Type",
                table: "Services",
                columns: new[] { "Name", "Type" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtest_Location",
                table: "Speedtests",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_BarangayId",
                table: "Speedtests",
                column: "BarangayId");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_FileId",
                table: "Speedtests",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_GenerationId",
                table: "Speedtests",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_ServiceProviderId",
                table: "Speedtests",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_TechnologyId",
                table: "Speedtests",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_AreaStudyId",
                table: "Studies",
                column: "AreaStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMunicipalities_MunicipalityId",
                table: "StudyMunicipalities",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMunicipalities_StudyId",
                table: "StudyMunicipalities",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNetworks_NetworkId",
                table: "StudyNetworks",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNetworks_StudyId",
                table: "StudyNetworks",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemTypes_GenerationId",
                table: "SystemTypes",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemTypes_TechnologyId",
                table: "SystemTypes",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_GenerationId",
                table: "Technologies",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Standard",
                table: "Technologies",
                column: "Standard",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyBands_BandId",
                table: "TechnologyBands",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyBands_TechnologyId_BandId",
                table: "TechnologyBands",
                columns: new[] { "TechnologyId", "BandId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegions_RegionId",
                table: "UserRegions",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegions_UserId",
                table: "UserRegions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserTypeId",
                table: "Users",
                column: "UserTypeId");

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

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_BarangayId",
                table: "WirelessValidations",
                column: "BarangayId");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_IsMarginal",
                table: "WirelessValidations",
                column: "IsMarginal");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_ServiceProviderId",
                table: "WirelessValidations",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_TechnologyId_BandId",
                table: "WirelessValidations",
                columns: new[] { "TechnologyId", "BandId" });

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidations_BandId",
                table: "WirelessValidations",
                column: "BandId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Zone_BarangayId",
                table: "Zone",
                column: "BarangayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CellularCoverages");

            migrationBuilder.DropTable(
                name: "Cellulars");

            migrationBuilder.DropTable(
                name: "ConnectivityAssessmentProvider");

            migrationBuilder.DropTable(
                name: "GenerationBands");

            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.DropTable(
                name: "NAPs");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropTable(
                name: "Speedtests");

            migrationBuilder.DropTable(
                name: "StudyMunicipalities");

            migrationBuilder.DropTable(
                name: "StudyNetworks");

            migrationBuilder.DropTable(
                name: "TechnologyBands");

            migrationBuilder.DropTable(
                name: "UserRegions");

            migrationBuilder.DropTable(
                name: "WiFiHotspots");

            migrationBuilder.DropTable(
                name: "WirelessValidations");

            migrationBuilder.DropTable(
                name: "Zone");

            migrationBuilder.DropTable(
                name: "ConnectivityAssessments");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "AntennaPatterns");

            migrationBuilder.DropTable(
                name: "CellSectors");

            migrationBuilder.DropTable(
                name: "MIMOs");

            migrationBuilder.DropTable(
                name: "Networks");

            migrationBuilder.DropTable(
                name: "Studies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Bands");

            migrationBuilder.DropTable(
                name: "Barangays");

            migrationBuilder.DropTable(
                name: "Cells");

            migrationBuilder.DropTable(
                name: "NetworkTemplates");

            migrationBuilder.DropTable(
                name: "SystemTypes");

            migrationBuilder.DropTable(
                name: "AreaStudies");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "UserTypes");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "ServiceProviders");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "Provinces");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Generations");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}
