using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredIndexToServiceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders",
                columns: new[] { "ServiceId", "ProviderId", "MNC" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ServiceId_ProviderId_MNC",
                table: "ServiceProviders",
                columns: new[] { "ServiceId", "ProviderId", "MNC" },
                unique: true);
        }
    }
}
