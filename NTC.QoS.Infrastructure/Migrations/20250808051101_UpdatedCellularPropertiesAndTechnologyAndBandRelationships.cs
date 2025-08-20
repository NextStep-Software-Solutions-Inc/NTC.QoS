using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedCellularPropertiesAndTechnologyAndBandRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_GenerationBands_GenerationBandId",
                table: "Cellulars");

            migrationBuilder.RenameColumn(
                name: "GenerationBandId",
                table: "Cellulars",
                newName: "TechnologyId");

            migrationBuilder.RenameIndex(
                name: "IX_Cellulars_GenerationBandId",
                table: "Cellulars",
                newName: "IX_Cellulars_TechnologyId");

            migrationBuilder.AddColumn<Guid>(
                name: "BandId",
                table: "Cellulars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "Bandwidth",
                table: "Cellulars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DLCenterFreq",
                table: "Cellulars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ULCenterFreq",
                table: "Cellulars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_BandId",
                table: "Cellulars",
                column: "BandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Bands_BandId",
                table: "Cellulars",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Technologies_TechnologyId",
                table: "Cellulars",
                column: "TechnologyId",
                principalTable: "Technologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Bands_BandId",
                table: "Cellulars");

            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Technologies_TechnologyId",
                table: "Cellulars");

            migrationBuilder.DropIndex(
                name: "IX_Cellulars_BandId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "BandId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "Bandwidth",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "DLCenterFreq",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "ULCenterFreq",
                table: "Cellulars");

            migrationBuilder.RenameColumn(
                name: "TechnologyId",
                table: "Cellulars",
                newName: "GenerationBandId");

            migrationBuilder.RenameIndex(
                name: "IX_Cellulars_TechnologyId",
                table: "Cellulars",
                newName: "IX_Cellulars_GenerationBandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_GenerationBands_GenerationBandId",
                table: "Cellulars",
                column: "GenerationBandId",
                principalTable: "GenerationBands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
