using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRegionEntityModelByAddingBoundariesCenterAndOtherAdditionalProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Area",
                table: "Regions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Regions",
                type: "geography (MultiPolygon,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.MultiPolygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;MULTIPOLYGON EMPTY"));

            migrationBuilder.AddColumn<Point>(
                name: "Center",
                table: "Regions",
                type: "geography (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Regions",
                type: "text",
                nullable: false,
                defaultValue: "");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Region_Boundaries",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Region_Center",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Boundaries",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Regions");
        }
    }
}
