using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMimoModelBuilderWithManySectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sectors_MIMOId",
                table: "Sectors");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_MIMOId",
                table: "Sectors",
                column: "MIMOId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sectors_MIMOId",
                table: "Sectors");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_MIMOId",
                table: "Sectors",
                column: "MIMOId",
                unique: true);
        }
    }
}
