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

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_Networks_ServiceProviderId_NormalizedName"
                ON "Networks" ("ServiceProviderId", lower(btrim("Name")))
                WHERE "IsDeleted" = FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Networks_ServiceProviderId_NormalizedName",
                table: "Networks");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_ServiceProviderId",
                table: "Networks",
                column: "ServiceProviderId");
        }
    }
}
