using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWirelessValidationCountFromProviderBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WirelessValidationCount",
                table: "ConnectivityAssessmentProvider");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WirelessValidationCount",
                table: "ConnectivityAssessmentProvider",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
