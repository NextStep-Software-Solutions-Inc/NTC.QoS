using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGeographyToGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Helper function to safely convert column type
            // Drops default constraints, converts type, then restores defaults if needed
            
            // WiFiHotspots - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'WiFiHotspots' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" TYPE geometry(Point,4326) USING ""Location""::geometry;
                    END IF;
                END $$;
            ");

            // Speedtests - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Speedtests' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" TYPE geometry(Point,4326) USING ""Location""::geometry;
                    END IF;
                END $$;
            ");

            // Regions - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Regions' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" TYPE geometry(Point,4326) USING ""Center""::geometry;
                    END IF;
                END $$;
            ");

            // Regions - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Regions' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon,4326) USING ""Boundaries""::geometry;
                    END IF;
                END $$;
            ");

            // Provinces - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Provinces' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" TYPE geometry(Point,4326) USING ""Center""::geometry;
                    END IF;
                END $$;
            ");

            // Provinces - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Provinces' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon,4326) USING ""Boundaries""::geometry;
                    END IF;
                END $$;
            ");

            // NAPs - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'NAPs' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" TYPE geometry(Point,4326) USING ""Location""::geometry;
                    END IF;
                END $$;
            ");

            // Municipalities - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Municipalities' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" TYPE geometry(Point,4326) USING ""Center""::geometry;
                    END IF;
                END $$;
            ");

            // Municipalities - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Municipalities' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon,4326) USING ""Boundaries""::geometry;
                    END IF;
                END $$;
            ");

            // ConnectivityAssessments - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ConnectivityAssessments' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" TYPE geometry(Point,4326) USING ""Location""::geometry;
                    END IF;
                END $$;
            ");

            // CellularCoverages - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'CellularCoverages' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" TYPE geometry(Point,4326) USING ""Location""::geometry;
                    END IF;
                END $$;
            ");

            // Cells - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Cells' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" TYPE geometry(Point,4326) USING ""Location""::geometry;
                    END IF;
                END $$;
            ");

            // Barangays - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Barangays' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" TYPE geometry(Point,4326) USING ""Center""::geometry;
                    END IF;
                END $$;
            ");

            // Barangays - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Barangays' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" TYPE geometry(MultiPolygon,4326) USING ""Boundaries""::geometry;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: geometry back to geography
            
            // WiFiHotspots - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'WiFiHotspots' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""WiFiHotspots"" ALTER COLUMN ""Location"" TYPE geography(Point,4326) USING ""Location""::geography;
                    END IF;
                END $$;
            ");

            // Speedtests - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Speedtests' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""Speedtests"" ALTER COLUMN ""Location"" TYPE geography(Point,4326) USING ""Location""::geography;
                    END IF;
                END $$;
            ");

            // Regions - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Regions' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Center"" TYPE geography(Point,4326) USING ""Center""::geography;
                    END IF;
                END $$;
            ");

            // Regions - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Regions' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Regions"" ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon,4326) USING ""Boundaries""::geography;
                    END IF;
                END $$;
            ");

            // Provinces - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Provinces' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Center"" TYPE geography(Point,4326) USING ""Center""::geography;
                    END IF;
                END $$;
            ");

            // Provinces - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Provinces' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Provinces"" ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon,4326) USING ""Boundaries""::geography;
                    END IF;
                END $$;
            ");

            // NAPs - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'NAPs' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""NAPs"" ALTER COLUMN ""Location"" TYPE geography(Point,4326) USING ""Location""::geography;
                    END IF;
                END $$;
            ");

            // Municipalities - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Municipalities' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Center"" TYPE geography(Point,4326) USING ""Center""::geography;
                    END IF;
                END $$;
            ");

            // Municipalities - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Municipalities' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Municipalities"" ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon,4326) USING ""Boundaries""::geography;
                    END IF;
                END $$;
            ");

            // ConnectivityAssessments - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ConnectivityAssessments' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""ConnectivityAssessments"" ALTER COLUMN ""Location"" TYPE geography(Point,4326) USING ""Location""::geography;
                    END IF;
                END $$;
            ");

            // CellularCoverages - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'CellularCoverages' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""CellularCoverages"" ALTER COLUMN ""Location"" TYPE geography(Point,4326) USING ""Location""::geography;
                    END IF;
                END $$;
            ");

            // Cells - Location
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Cells' AND column_name = 'Location'
                    ) THEN
                        ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" DROP DEFAULT;
                        ALTER TABLE ""Cells"" ALTER COLUMN ""Location"" TYPE geography(Point,4326) USING ""Location""::geography;
                    END IF;
                END $$;
            ");

            // Barangays - Center
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Barangays' AND column_name = 'Center'
                    ) THEN
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" DROP DEFAULT;
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Center"" TYPE geography(Point,4326) USING ""Center""::geography;
                    END IF;
                END $$;
            ");

            // Barangays - Boundaries
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Barangays' AND column_name = 'Boundaries'
                    ) THEN
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" DROP DEFAULT;
                        ALTER TABLE ""Barangays"" ALTER COLUMN ""Boundaries"" TYPE geography(MultiPolygon,4326) USING ""Boundaries""::geography;
                    END IF;
                END $$;
            ");
        }
    }
}
