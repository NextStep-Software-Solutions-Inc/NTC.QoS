using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsRequiredFalseInNAP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NAPs_Files_FileId",
                table: "NAPs");

            migrationBuilder.DropForeignKey(
                name: "FK_NAPs_Zone_ZoneId",
                table: "NAPs");

            migrationBuilder.AddForeignKey(
                name: "FK_NAPs_Files_FileId",
                table: "NAPs",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NAPs_Zone_ZoneId",
                table: "NAPs",
                column: "ZoneId",
                principalTable: "Zone",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NAPs_Files_FileId",
                table: "NAPs");

            migrationBuilder.DropForeignKey(
                name: "FK_NAPs_Zone_ZoneId",
                table: "NAPs");

            migrationBuilder.AddForeignKey(
                name: "FK_NAPs_Files_FileId",
                table: "NAPs",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NAPs_Zone_ZoneId",
                table: "NAPs",
                column: "ZoneId",
                principalTable: "Zone",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
