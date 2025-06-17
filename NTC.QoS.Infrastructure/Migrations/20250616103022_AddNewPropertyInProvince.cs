using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewPropertyInProvince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Area",
                table: "Provinces",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<MultiPolygon>(
                name: "Boundaries",
                table: "Provinces",
                type: "geometry (MultiPolygon, 4326)",
                nullable: false);

            migrationBuilder.AddColumn<Point>(
                name: "Center",
                table: "Provinces",
                type: "geography (Point, 4326)",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Provinces",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Province_Boundaries_GIST",
                table: "Provinces",
                column: "Boundaries")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Province_Center_GIST",
                table: "Provinces",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Province_Boundaries_GIST",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Province_Center_GIST",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "Boundaries",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Provinces");
        }
    }
}
