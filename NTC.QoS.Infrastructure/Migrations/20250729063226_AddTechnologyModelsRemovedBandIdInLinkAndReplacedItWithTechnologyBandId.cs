using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnologyModelsRemovedBandIdInLinkAndReplacedItWithTechnologyBandId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_Bands_BandId",
                table: "Links");

            migrationBuilder.RenameColumn(
                name: "BandId",
                table: "Links",
                newName: "TechnologyBandId");

            migrationBuilder.RenameIndex(
                name: "IX_Links_BandId",
                table: "Links",
                newName: "IX_Links_TechnologyBandId");

            migrationBuilder.CreateTable(
                name: "TechnologyBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyBands_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologyBands_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyBands_BandId",
                table: "TechnologyBands",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyBands_TechnologyId_BandId",
                table: "TechnologyBands",
                columns: new[] { "TechnologyId", "BandId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_TechnologyBands_TechnologyBandId",
                table: "Links",
                column: "TechnologyBandId",
                principalTable: "TechnologyBands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_TechnologyBands_TechnologyBandId",
                table: "Links");

            migrationBuilder.DropTable(
                name: "TechnologyBands");

            migrationBuilder.RenameColumn(
                name: "TechnologyBandId",
                table: "Links",
                newName: "BandId");

            migrationBuilder.RenameIndex(
                name: "IX_Links_TechnologyBandId",
                table: "Links",
                newName: "IX_Links_BandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Bands_BandId",
                table: "Links",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
