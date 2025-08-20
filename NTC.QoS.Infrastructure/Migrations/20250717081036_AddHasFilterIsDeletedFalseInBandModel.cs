using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHasFilterIsDeletedFalseInBandModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bands_Name",
                table: "Bands");

            migrationBuilder.CreateIndex(
                name: "IX_Bands_Name",
                table: "Bands",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bands_Name",
                table: "Bands");

            migrationBuilder.CreateIndex(
                name: "IX_Bands_Name",
                table: "Bands",
                column: "Name",
                unique: true);
        }
    }
}
