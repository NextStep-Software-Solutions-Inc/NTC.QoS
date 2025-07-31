using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProviderToNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProviderId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Networks_ServiceProviderId",
                table: "Networks",
                column: "ServiceProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_ServiceProviders_ServiceProviderId",
                table: "Networks",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_ServiceProviders_ServiceProviderId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_ServiceProviderId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "Networks");
        }
    }
}
