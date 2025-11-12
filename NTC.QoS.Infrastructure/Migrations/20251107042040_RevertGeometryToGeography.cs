using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevertGeometryToGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert WiFiHotspots.Location from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""WiFiHotspots"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Speedtests.Location from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Speedtests"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Regions.Center from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
          ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Regions.Boundaries from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");

            // Convert Provinces.Center from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Provinces.Boundaries from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");

            // Convert NAPs.Location from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""NAPs"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Municipalities.Center from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Municipalities.Boundaries from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");

            // Convert ConnectivityAssessments.Location from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""ConnectivityAssessments"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert CellularCoverages.Location from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""CellularCoverages"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Cells.Location from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Cells"" 
                ALTER COLUMN ""Location"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Location""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Barangays.Center from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Center"" TYPE geography(Point, 4326) 
                USING ST_SetSRID(""Center""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326)::geography;");

            // Convert Barangays.Boundaries from geometry to geography
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geography, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326)::geography;");
        }

      /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert WiFiHotspots.Location from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""WiFiHotspots"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Speedtests.Location from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Speedtests"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Regions.Center from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Regions.Boundaries from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");

            // Revert Provinces.Center from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Provinces.Boundaries from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Provinces"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");

            // Revert NAPs.Location from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""NAPs"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Municipalities.Center from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Municipalities.Boundaries from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Municipalities"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");

            // Revert ConnectivityAssessments.Location from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""ConnectivityAssessments"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert CellularCoverages.Location from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""CellularCoverages"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Cells.Location from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Cells"" 
                ALTER COLUMN ""Location"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Location""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Barangays.Center from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Center"" TYPE geometry(Point, 4326) 
                USING ST_SetSRID(""Center""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" SET DEFAULT ST_SetSRID(ST_MakePoint(0, 0), 4326);");

            // Revert Barangays.Boundaries from geography to geometry
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Barangays"" 
                ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon, 4326) 
                USING ST_SetSRID(""Boundaries""::geometry, 4326);
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" SET DEFAULT ST_GeomFromText('MULTIPOLYGON EMPTY', 4326);");
        }
    }
}
