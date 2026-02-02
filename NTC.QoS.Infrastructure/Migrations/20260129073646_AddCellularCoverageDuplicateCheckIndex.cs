using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCellularCoverageDuplicateCheckIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_NetworkId",
                table: "CellularCoverages");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverage_DuplicateCheck",
                table: "CellularCoverages",
                columns: new[] { "NetworkId", "BandId", "MunicipalityId", "IsDeleted" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CellularCoverage_DuplicateCheck",
                table: "CellularCoverages");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_NetworkId",
                table: "CellularCoverages",
                column: "NetworkId");
        }
    }
}
