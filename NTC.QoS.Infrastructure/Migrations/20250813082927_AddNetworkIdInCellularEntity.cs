using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkIdInCellularEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NetworkId",
                table: "Cellulars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Cellulars_NetworkId",
                table: "Cellulars",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Networks_NetworkId",
                table: "Cellulars",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Networks_NetworkId",
                table: "Cellulars");

            migrationBuilder.DropIndex(
                name: "IX_Cellulars_NetworkId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "NetworkId",
                table: "Cellulars");
        }
    }
}
