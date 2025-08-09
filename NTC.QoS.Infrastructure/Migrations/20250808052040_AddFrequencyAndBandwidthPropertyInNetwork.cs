using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFrequencyAndBandwidthPropertyInNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_NetworkTemplates_NetworkTemplateId",
                table: "Networks");

            migrationBuilder.AlterColumn<Guid>(
                name: "NetworkTemplateId",
                table: "Networks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "Bandwidth",
                table: "Networks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DLCenterFreq",
                table: "Networks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ULCenterFreq",
                table: "Networks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_NetworkTemplates_NetworkTemplateId",
                table: "Networks",
                column: "NetworkTemplateId",
                principalTable: "NetworkTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_NetworkTemplates_NetworkTemplateId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "Bandwidth",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "DLCenterFreq",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "ULCenterFreq",
                table: "Networks");

            migrationBuilder.AlterColumn<Guid>(
                name: "NetworkTemplateId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_NetworkTemplates_NetworkTemplateId",
                table: "Networks",
                column: "NetworkTemplateId",
                principalTable: "NetworkTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
