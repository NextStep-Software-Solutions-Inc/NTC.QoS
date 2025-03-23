using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSectorEntityRxAntennaPatternIdFromNullableToNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sectors_AntennaPatterns_RxAntennaPatternId",
                table: "Sectors");

            migrationBuilder.AlterColumn<Guid>(
                name: "RxAntennaPatternId",
                table: "Sectors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sectors_AntennaPatterns_RxAntennaPatternId",
                table: "Sectors",
                column: "RxAntennaPatternId",
                principalTable: "AntennaPatterns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sectors_AntennaPatterns_RxAntennaPatternId",
                table: "Sectors");

            migrationBuilder.AlterColumn<Guid>(
                name: "RxAntennaPatternId",
                table: "Sectors",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Sectors_AntennaPatterns_RxAntennaPatternId",
                table: "Sectors",
                column: "RxAntennaPatternId",
                principalTable: "AntennaPatterns",
                principalColumn: "Id");
        }
    }
}
