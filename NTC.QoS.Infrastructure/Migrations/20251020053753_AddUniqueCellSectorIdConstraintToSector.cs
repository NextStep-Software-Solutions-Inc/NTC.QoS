using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCellSectorIdConstraintToSector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sectors_CellSectorId",
                table: "Sectors");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_CellSectorId",
                table: "Sectors",
                column: "CellSectorId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sectors_CellSectorId",
                table: "Sectors");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_CellSectorId",
                table: "Sectors",
                column: "CellSectorId");
        }
    }
}
