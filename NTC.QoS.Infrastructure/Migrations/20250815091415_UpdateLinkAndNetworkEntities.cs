using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLinkNetworkAndCellularEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Links_DownLinkId",
                table: "Networks");

            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Links_UpLinkId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_DownLinkId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_UpLinkId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "DownLinkId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "UpLinkId",
                table: "Networks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DownLinkId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpLinkId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Networks_DownLinkId",
                table: "Networks",
                column: "DownLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_UpLinkId",
                table: "Networks",
                column: "UpLinkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Links_DownLinkId",
                table: "Networks",
                column: "DownLinkId",
                principalTable: "Links",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Links_UpLinkId",
                table: "Networks",
                column: "UpLinkId",
                principalTable: "Links",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
