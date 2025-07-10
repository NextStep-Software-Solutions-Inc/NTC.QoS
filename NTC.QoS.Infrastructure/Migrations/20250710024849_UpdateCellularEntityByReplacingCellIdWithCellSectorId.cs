using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCellularEntityByReplacingCellIdWithCellSectorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Cells_CellId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "CellId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "BarangayId",
                table: "Cellulars");

            migrationBuilder.AddColumn<Guid>(
                name: "CellSectorId",
                table: "Cellulars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars",
                column: "CellSectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_CellSectors_CellSectorId",
                table: "Cellulars",
                column: "CellSectorId",
                principalTable: "CellSectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_CellSectors_CellSectorId",
                table: "Cellulars");

            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Cells_CellId",
                table: "Cellulars");

            migrationBuilder.DropIndex(
                name: "IX_Cellulars_CellSectorId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "CellSectorId",
                table: "Cellulars");

            migrationBuilder.AlterColumn<Guid>(
                name: "CellId",
                table: "Cellulars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Cells_CellId",
                table: "Cellulars",
                column: "CellId",
                principalTable: "Cells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
