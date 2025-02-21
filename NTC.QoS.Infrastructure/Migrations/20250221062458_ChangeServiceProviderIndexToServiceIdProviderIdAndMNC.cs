using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeServiceProviderIndexToServiceIdProviderIdAndMNC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId",
                table: "ServiceProviders");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders",
                columns: new[] { "ServiceId", "ProviderId", "MNC" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId",
                table: "ServiceProviders",
                columns: new[] { "ServiceId", "ProviderId" },
                unique: true);
        }
    }
}
