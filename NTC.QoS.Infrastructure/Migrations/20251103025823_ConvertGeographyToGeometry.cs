using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertGeographyToGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert WiFiHotspots.Location
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""WiFiHotspots"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Speedtests.Location
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Speedtests"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Regions.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Regions.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");

            // Convert Provinces.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Provinces.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");

            // Convert NAPs.Location
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""NAPs"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Municipalities.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Municipalities.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");

            // Convert ConnectivityAssessments.Location
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""ConnectivityAssessments"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert CellularCoverages.Location
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""CellularCoverages"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Cells.Location
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Cells"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Barangays.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Convert Barangays.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert WiFiHotspots.Location
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""WiFiHotspots"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Speedtests.Location
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Speedtests"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Regions.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Regions.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");

            // Revert Provinces.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Provinces.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");

            // Revert NAPs.Location
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""NAPs"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Municipalities.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Municipalities.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");

            // Revert ConnectivityAssessments.Location
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""ConnectivityAssessments"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert CellularCoverages.Location
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""CellularCoverages"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Cells.Location
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Cells"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Barangays.Center
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Revert Barangays.Boundaries
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");
        }
    }
}
