using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredIndexesCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_Name_Type",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Providers_Name",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Cells_Name",
                table: "Cells");

            migrationBuilder.DropIndex(
                name: "IX_AreaStudies_Name",
                table: "AreaStudies");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name_Type",
                table: "Services",
                columns: new[] { "Name", "Type" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Name",
                table: "Providers",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Cells_Name",
                table: "Cells",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AreaStudies_Name",
                table: "AreaStudies",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_Name_Type",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Providers_Name",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Cells_Name",
                table: "Cells");

            migrationBuilder.DropIndex(
                name: "IX_AreaStudies_Name",
                table: "AreaStudies");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name_Type",
                table: "Services",
                columns: new[] { "Name", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Name",
                table: "Providers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cells_Name",
                table: "Cells",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaStudies_Name",
                table: "AreaStudies",
                column: "Name",
                unique: true);
        }
    }
}
