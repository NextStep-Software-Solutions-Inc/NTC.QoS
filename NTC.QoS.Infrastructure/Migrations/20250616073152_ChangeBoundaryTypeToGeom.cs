using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBoundaryTypeToGeom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" DROP COLUMN ""Boundaries"";
                ALTER TABLE ""Regions"" ADD COLUMN ""Boundaries"" geometry(MultiPolygon, 4326);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Regions"" DROP COLUMN ""Boundaries"";
                ALTER TABLE ""Regions"" ADD COLUMN ""Boundaries"" geography(MultiPolygon, 4326);
            ");
        }
    }
}
