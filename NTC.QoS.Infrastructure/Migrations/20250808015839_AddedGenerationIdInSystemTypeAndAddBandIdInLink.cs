using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedGenerationIdInSystemTypeAndAddBandIdInLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_TechnologyBands_TechnologyBandId",
                table: "Links");

            migrationBuilder.AddColumn<Guid>(
                name: "GenerationId",
                table: "SystemTypes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "TechnologyBandId",
                table: "Links",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "BandId",
                table: "Links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SystemTypes_GenerationId",
                table: "SystemTypes",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_BandId",
                table: "Links",
                column: "BandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Bands_BandId",
                table: "Links",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Links_TechnologyBands_TechnologyBandId",
                table: "Links",
                column: "TechnologyBandId",
                principalTable: "TechnologyBands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemTypes_Generations_GenerationId",
                table: "SystemTypes",
                column: "GenerationId",
                principalTable: "Generations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_Bands_BandId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_Links_TechnologyBands_TechnologyBandId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemTypes_Generations_GenerationId",
                table: "SystemTypes");

            migrationBuilder.DropIndex(
                name: "IX_SystemTypes_GenerationId",
                table: "SystemTypes");

            migrationBuilder.DropIndex(
                name: "IX_Links_BandId",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "GenerationId",
                table: "SystemTypes");

            migrationBuilder.DropColumn(
                name: "BandId",
                table: "Links");

            migrationBuilder.AlterColumn<Guid>(
                name: "TechnologyBandId",
                table: "Links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Links_TechnologyBands_TechnologyBandId",
                table: "Links",
                column: "TechnologyBandId",
                principalTable: "TechnologyBands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
