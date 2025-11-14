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
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "WiFiHotspots",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Speedtests",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Regions",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Regions",
                type: "geometry (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Provinces",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geometry (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "NAPs",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Municipalities",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Municipalities",
                type: "geometry (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "ConnectivityAssessments",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "CellularCoverages",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Cells",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Barangays",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geography (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Barangays",
                type: "geometry (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "WiFiHotspots",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Speedtests",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Regions",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Regions",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geometry (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Provinces",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geometry (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "NAPs",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Municipalities",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Municipalities",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geometry (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "ConnectivityAssessments",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "CellularCoverages",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Cells",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<Point>(
                name: "Center",
                table: "Barangays",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Barangays",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geometry (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));
        }
    }
}
