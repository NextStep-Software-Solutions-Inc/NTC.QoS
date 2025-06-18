using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedGeoEntitiesPropertiesAndRemovedVirtualColumnOrCastingOfGeographyToGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Provinces_Regions_RegionId",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "BoundariesGeometry",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "CenterGeometry",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "BoundariesGeometry",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "CenterGeometry",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "LocationGeometry",
                table: "Cells");

            migrationBuilder.AddColumn<float>(
                name: "Area",
                table: "Municipalities",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Municipalities",
                type: "geography (MultiPolygon,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AddColumn<Point>(
                name: "Center",
                table: "Municipalities",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Municipalities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "Area",
                table: "Barangays",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Barangays",
                type: "geography (MultiPolygon,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AddColumn<Point>(
                name: "Center",
                table: "Barangays",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Barangays",
                type: "text",
                nullable: false,
                defaultValue: "");

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
                name: "IX_Barangay_Boundaries",
                table: "Barangays",
                column: "Boundaries")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Barangay_Center",
                table: "Barangays",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.AddForeignKey(
                name: "FK_Provinces_Regions_RegionId",
                table: "Provinces",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Provinces_Regions_RegionId",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Municipality_Boundaries",
                table: "Municipalities");

            migrationBuilder.DropIndex(
                name: "IX_Municipality_Center",
                table: "Municipalities");

            migrationBuilder.DropIndex(
                name: "IX_Barangay_Boundaries",
                table: "Barangays");

            migrationBuilder.DropIndex(
                name: "IX_Barangay_Center",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Boundaries",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "Boundaries",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Barangays");

            migrationBuilder.AddColumn<MultiPolygon>(
                name: "BoundariesGeometry",
                table: "Regions",
                type: "geometry",
                nullable: false,
                computedColumnSql: "\"Boundaries\"::geometry",
                stored: true);

            migrationBuilder.AddColumn<Point>(
                name: "CenterGeometry",
                table: "Regions",
                type: "geometry",
                nullable: false,
                computedColumnSql: "\"Center\"::geometry",
                stored: true);

            migrationBuilder.AddColumn<MultiPolygon>(
                name: "BoundariesGeometry",
                table: "Provinces",
                type: "geometry",
                nullable: false,
                computedColumnSql: "\"Boundaries\"::geometry",
                stored: true);

            migrationBuilder.AddColumn<Point>(
                name: "CenterGeometry",
                table: "Provinces",
                type: "geometry",
                nullable: false,
                computedColumnSql: "\"Center\"::geometry",
                stored: true);

            migrationBuilder.AddColumn<Point>(
                name: "LocationGeometry",
                table: "Cells",
                type: "geometry",
                nullable: false,
                computedColumnSql: "\"Location\"::geometry",
                stored: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Provinces_Regions_RegionId",
                table: "Provinces",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id");
        }
    }
}
