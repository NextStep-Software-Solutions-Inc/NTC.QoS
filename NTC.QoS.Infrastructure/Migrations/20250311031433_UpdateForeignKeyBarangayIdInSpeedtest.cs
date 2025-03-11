using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForeignKeyBarangayIdInSpeedtest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Barangays_GenerationId",
                table: "Speedtests");

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_BarangayId",
                table: "Speedtests",
                column: "BarangayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Barangays_BarangayId",
                table: "Speedtests",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Barangays_BarangayId",
                table: "Speedtests");

            migrationBuilder.DropIndex(
                name: "IX_Speedtests_BarangayId",
                table: "Speedtests");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Barangays_GenerationId",
                table: "Speedtests",
                column: "GenerationId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
