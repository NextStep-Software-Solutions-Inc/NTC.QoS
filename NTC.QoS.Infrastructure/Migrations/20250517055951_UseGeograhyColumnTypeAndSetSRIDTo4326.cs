using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseGeographyColumnTypeAndSetSRIDTo4326 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {  

            // Revert the SRID back to the previous value (e.g., 4326)
            migrationBuilder.Sql(
                "UPDATE \"NAPs\" " +
                "SET \"Location\" = ST_Transform(ST_SetSRID(\"Location\", 3857), 4326) " +
                "WHERE \"Location\" IS NOT NULL;"
            );

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "NAPs",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=3857;POINT (0 0)"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "NAPs",
                type: "geometry",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=3857;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            // Update the SRID of the Location column in the NAPs table
            migrationBuilder.Sql(
                "UPDATE \"NAPs\" " +
                "SET \"Location\" = ST_Transform(ST_SetSRID(\"Location\", 4326), 3857) " +
                "WHERE \"Location\" IS NOT NULL;"
            );
        }
    }
}
