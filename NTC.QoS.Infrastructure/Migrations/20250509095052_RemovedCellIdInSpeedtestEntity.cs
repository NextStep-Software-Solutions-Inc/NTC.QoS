using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedCellIdInSpeedtestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Cells_CellId",
                table: "Speedtests");

            migrationBuilder.DropIndex(
                name: "IX_Speedtests_CellId",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "CellId",
                table: "Speedtests");

            migrationBuilder.AddColumn<string>(
                name: "CellIdentifier",
                table: "Speedtests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellIdentifier",
                table: "Speedtests");

            migrationBuilder.AddColumn<Guid>(
                name: "CellId",
                table: "Speedtests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_CellId",
                table: "Speedtests",
                column: "CellId");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Cells_CellId",
                table: "Speedtests",
                column: "CellId",
                principalTable: "Cells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
