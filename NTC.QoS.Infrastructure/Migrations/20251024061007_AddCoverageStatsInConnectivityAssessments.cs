using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverageStatsInConnectivityAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AssetsPerThousandPeople",
                table: "ConnectivityAssessments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedHouseholdCoveragePercent",
                table: "ConnectivityAssessments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedPopulationCoveragePercent",
                table: "ConnectivityAssessments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProvidersPerThousandPeople",
                table: "ConnectivityAssessments",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetsPerThousandPeople",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "EstimatedHouseholdCoveragePercent",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "EstimatedPopulationCoveragePercent",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "ProvidersPerThousandPeople",
                table: "ConnectivityAssessments");
        }
    }
}
