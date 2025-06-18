using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeometryComputedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
