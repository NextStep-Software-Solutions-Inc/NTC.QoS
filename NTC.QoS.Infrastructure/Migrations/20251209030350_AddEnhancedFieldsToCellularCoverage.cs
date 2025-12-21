using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedFieldsToCellularCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BandId",
                table: "CellularCoverages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CellSectorId",
                table: "CellularCoverages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MunicipalityId",
                table: "CellularCoverages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProviderId",
                table: "CellularCoverages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnologyId",
                table: "CellularCoverages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_BandId",
                table: "CellularCoverages",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_CellSectorId",
                table: "CellularCoverages",
                column: "CellSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_MunicipalityId",
                table: "CellularCoverages",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_ServiceProviderId",
                table: "CellularCoverages",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverages_TechnologyId",
                table: "CellularCoverages",
                column: "TechnologyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoverages_Bands_BandId",
                table: "CellularCoverages",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoverages_CellSectors_CellSectorId",
                table: "CellularCoverages",
                column: "CellSectorId",
                principalTable: "CellSectors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoverages_Municipalities_MunicipalityId",
                table: "CellularCoverages",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoverages_ServiceProviders_ServiceProviderId",
                table: "CellularCoverages",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoverages_Technologies_TechnologyId",
                table: "CellularCoverages",
                column: "TechnologyId",
                principalTable: "Technologies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_Bands_BandId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_CellSectors_CellSectorId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_Municipalities_MunicipalityId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_ServiceProviders_ServiceProviderId",
                table: "CellularCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoverages_Technologies_TechnologyId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_BandId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_CellSectorId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_MunicipalityId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_ServiceProviderId",
                table: "CellularCoverages");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverages_TechnologyId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "BandId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "CellSectorId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "CellularCoverages");

            migrationBuilder.DropColumn(
                name: "TechnologyId",
                table: "CellularCoverages");
        }
    }
}
