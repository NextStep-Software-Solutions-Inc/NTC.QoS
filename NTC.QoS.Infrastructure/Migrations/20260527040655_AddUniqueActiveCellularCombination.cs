using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveCellularCombination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band",
                table: "Cellulars",
                columns: new[] { "CellSectorId", "ServiceProviderId", "TechnologyId", "BandId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band",
                table: "Cellulars");

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars",
                column: "CellSectorId");
        }
    }
}
