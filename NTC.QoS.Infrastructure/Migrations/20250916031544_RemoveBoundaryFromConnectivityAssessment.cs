using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBoundaryFromConnectivityAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConnectivityAssessment_Boundary",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "Boundary",
                table: "ConnectivityAssessments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<MultiPolygon>(
                name: "Boundary",
                table: "ConnectivityAssessments",
                type: "geography (MultiPolygon,4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Boundary",
                table: "ConnectivityAssessments",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }
    }
}
