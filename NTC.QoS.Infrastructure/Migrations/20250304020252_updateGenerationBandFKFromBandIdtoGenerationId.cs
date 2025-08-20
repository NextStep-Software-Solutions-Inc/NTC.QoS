using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateGenerationBandFKFromBandIdtoGenerationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenerationBands_Generations_BandId",
                table: "GenerationBands");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationBands_GenerationId",
                table: "GenerationBands",
                column: "GenerationId");

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationBands_Generations_GenerationId",
                table: "GenerationBands",
                column: "GenerationId",
                principalTable: "Generations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenerationBands_Generations_GenerationId",
                table: "GenerationBands");

            migrationBuilder.DropIndex(
                name: "IX_GenerationBands_GenerationId",
                table: "GenerationBands");

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationBands_Generations_BandId",
                table: "GenerationBands",
                column: "BandId",
                principalTable: "Generations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
