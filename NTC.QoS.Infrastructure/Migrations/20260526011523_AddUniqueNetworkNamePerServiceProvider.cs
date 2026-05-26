using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueNetworkNamePerServiceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Networks_ServiceProviderId",
                table: "Networks");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_ServiceProviderId_Name",
                table: "Networks",
                columns: new[] { "ServiceProviderId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Networks_ServiceProviderId_Name",
                table: "Networks");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_ServiceProviderId",
                table: "Networks",
                column: "ServiceProviderId");
        }
    }
}
