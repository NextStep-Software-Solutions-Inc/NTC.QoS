using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProvinceBoundaryColumnTypeToGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geography (MultiPolygon, 4326)",
                nullable: false,
                oldClrType: typeof(MultiPolygon),
                oldType: "geometry (MultiPolygon, 4326)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geometry (MultiPolygon, 4326)",
                nullable: false,
                oldClrType: typeof(MultiPolygon),
                oldType: "geography (MultiPolygon, 4326)");
        }
    }
}
