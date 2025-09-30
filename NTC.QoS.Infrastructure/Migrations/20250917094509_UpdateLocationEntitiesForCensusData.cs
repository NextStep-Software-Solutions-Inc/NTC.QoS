using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocationEntitiesForCensusData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Population",
                table: "Regions",
                newName: "TotalPopulation");

            migrationBuilder.RenameColumn(
                name: "Population",
                table: "Provinces",
                newName: "TotalPopulation");

            migrationBuilder.RenameColumn(
                name: "Population",
                table: "Municipalities",
                newName: "TotalPopulation");

            migrationBuilder.RenameColumn(
                name: "Population",
                table: "Barangays",
                newName: "TotalPopulation");

            migrationBuilder.AddColumn<int>(
                name: "HouseholdPopulation",
                table: "Regions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdPopulation",
                table: "Provinces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdPopulation",
                table: "Municipalities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdPopulation",
                table: "Barangays",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HouseholdPopulation",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "HouseholdPopulation",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "HouseholdPopulation",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "HouseholdPopulation",
                table: "Barangays");

            migrationBuilder.RenameColumn(
                name: "TotalPopulation",
                table: "Regions",
                newName: "Population");

            migrationBuilder.RenameColumn(
                name: "TotalPopulation",
                table: "Provinces",
                newName: "Population");

            migrationBuilder.RenameColumn(
                name: "TotalPopulation",
                table: "Municipalities",
                newName: "Population");

            migrationBuilder.RenameColumn(
                name: "TotalPopulation",
                table: "Barangays",
                newName: "Population");
        }
    }
}
