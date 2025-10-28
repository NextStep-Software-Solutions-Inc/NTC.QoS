using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixConnectivityAssessmentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConnectivityAssessment_Location_Type",
                table: "ConnectivityAssessments");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_LocationId",
                table: "ConnectivityAssessments",
                column: "LocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_LocationType",
                table: "ConnectivityAssessments",
                column: "LocationType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConnectivityAssessment_LocationId",
                table: "ConnectivityAssessments");

            migrationBuilder.DropIndex(
                name: "IX_ConnectivityAssessment_LocationType",
                table: "ConnectivityAssessments");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessment_Location_Type",
                table: "ConnectivityAssessments",
                columns: new[] { "LocationId", "LocationType" },
                unique: true);
        }
    }
}
