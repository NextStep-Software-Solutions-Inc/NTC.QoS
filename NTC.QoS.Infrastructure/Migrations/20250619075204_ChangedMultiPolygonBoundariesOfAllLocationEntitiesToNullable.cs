using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedMultiPolygonBoundariesOfAllLocationEntitiesToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Regions",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Municipalities",
                type: "geography (MultiPolygon,4326)",
                nullable: true,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Regions",
                type: "geography (MultiPolygon,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geography (MultiPolygon,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Municipalities",
                type: "geography (MultiPolygon,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"),
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon,4326)",
                oldNullable: true,
                oldDefaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));
        }
    }
}
