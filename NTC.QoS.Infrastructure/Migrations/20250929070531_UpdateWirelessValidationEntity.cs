using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWirelessValidationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WirelessValidations_Barangays_BarangayId",
                table: "WirelessValidations");

            migrationBuilder.RenameColumn(
                name: "RSSI",
                table: "WirelessValidations",
                newName: "RSRP");

            migrationBuilder.AddForeignKey(
                name: "FK_WirelessValidations_Barangays_BarangayId",
                table: "WirelessValidations",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WirelessValidations_Barangays_BarangayId",
                table: "WirelessValidations");

            migrationBuilder.RenameColumn(
                name: "RSRP",
                table: "WirelessValidations",
                newName: "RSSI");

            migrationBuilder.AddForeignKey(
                name: "FK_WirelessValidations_Barangays_BarangayId",
                table: "WirelessValidations",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
