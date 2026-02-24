using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionIdToFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RegionId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_RegionId",
                table: "Files",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Regions_RegionId",
                table: "Files",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Regions_RegionId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_RegionId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Files");
        }
    }
}
