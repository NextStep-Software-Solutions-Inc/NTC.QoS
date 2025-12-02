using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBandIdAndMunicipalityIdToNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete existing data that references Networks
            // Note: This is safe for development. In production, you'd want a proper data migration strategy.
            migrationBuilder.Sql("DELETE FROM \"Sectors\";");
            migrationBuilder.Sql("DELETE FROM \"Cellulars\";");
            migrationBuilder.Sql("DELETE FROM \"CellularCoverages\";");
            migrationBuilder.Sql("DELETE FROM \"StudyNetworks\";");
            migrationBuilder.Sql("DELETE FROM \"Networks\";");

            migrationBuilder.AddColumn<Guid>(
                name: "BandId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MunicipalityId",
                table: "Networks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Networks_BandId",
                table: "Networks",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_MunicipalityId",
                table: "Networks",
                column: "MunicipalityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Bands_BandId",
                table: "Networks",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Municipalities_MunicipalityId",
                table: "Networks",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Bands_BandId",
                table: "Networks");

            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Municipalities_MunicipalityId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_BandId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_MunicipalityId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "BandId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Networks");
        }
    }
}
