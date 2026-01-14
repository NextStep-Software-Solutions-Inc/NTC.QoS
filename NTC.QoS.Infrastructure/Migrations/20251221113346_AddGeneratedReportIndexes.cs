using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GeneratedReports_UserId",
                table: "GeneratedReports");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_Title",
                table: "GeneratedReports",
                column: "Title",
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_UserId_GeneratedAt",
                table: "GeneratedReports",
                columns: new[] { "UserId", "GeneratedAt" },
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GeneratedReport_Title",
                table: "GeneratedReports");

            migrationBuilder.DropIndex(
                name: "IX_GeneratedReport_UserId_GeneratedAt",
                table: "GeneratedReports");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_UserId",
                table: "GeneratedReports",
                column: "UserId");
        }
    }
}
