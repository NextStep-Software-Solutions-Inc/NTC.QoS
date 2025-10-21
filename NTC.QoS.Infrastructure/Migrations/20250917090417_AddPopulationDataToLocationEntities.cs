using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPopulationDataToLocationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HouseholdCount",
                table: "Regions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Regions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdCount",
                table: "Provinces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Provinces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdCount",
                table: "Municipalities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Municipalities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdCount",
                table: "Barangays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Barangays",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HouseholdCount",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Population",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "HouseholdCount",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "Population",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "HouseholdCount",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Population",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "HouseholdCount",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "Population",
                table: "Barangays");
        }
    }
}
