using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingPropertiesInSpeedtestAndAddingTechnologyIdAndIsFixedBroadBandProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Generations_GenerationId",
                table: "Speedtests");

            migrationBuilder.AlterColumn<string>(
                name: "SignalQuality",
                table: "Speedtests",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "RSSI",
                table: "Speedtests",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<Guid>(
                name: "GenerationId",
                table: "Speedtests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "IsFixedBroadband",
                table: "Speedtests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnologyId",
                table: "Speedtests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Speedtests_TechnologyId",
                table: "Speedtests",
                column: "TechnologyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Generations_GenerationId",
                table: "Speedtests",
                column: "GenerationId",
                principalTable: "Generations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Technologies_TechnologyId",
                table: "Speedtests",
                column: "TechnologyId",
                principalTable: "Technologies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Generations_GenerationId",
                table: "Speedtests");

            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Technologies_TechnologyId",
                table: "Speedtests");

            migrationBuilder.DropIndex(
                name: "IX_Speedtests_TechnologyId",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "IsFixedBroadband",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "TechnologyId",
                table: "Speedtests");

            migrationBuilder.AlterColumn<int>(
                name: "SignalQuality",
                table: "Speedtests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RSSI",
                table: "Speedtests",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GenerationId",
                table: "Speedtests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Generations_GenerationId",
                table: "Speedtests",
                column: "GenerationId",
                principalTable: "Generations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
