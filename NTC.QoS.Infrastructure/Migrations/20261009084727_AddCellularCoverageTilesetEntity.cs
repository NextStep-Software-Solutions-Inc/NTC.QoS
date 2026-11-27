using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCellularCoverageTilesetEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CellularCoverageTilesets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    IngestId = table.Column<string>(type: "text", nullable: false),
                    DocumentId = table.Column<string>(type: "text", nullable: true),
                    TileJsonUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    BandId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellularCoverageTilesets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellularCoverageTilesets_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CellularCoverageTilesets_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CellularCoverageTilesets_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CellularCoverageTilesets_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_BandId",
                table: "CellularCoverageTilesets",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_DocumentId",
                table: "CellularCoverageTilesets",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_IngestId",
                table: "CellularCoverageTilesets",
                column: "IngestId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_IsActive",
                table: "CellularCoverageTilesets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_RegionId",
                table: "CellularCoverageTilesets",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_ServiceProviderId",
                table: "CellularCoverageTilesets",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_Status",
                table: "CellularCoverageTilesets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageTilesets_TechnologyId",
                table: "CellularCoverageTilesets",
                column: "TechnologyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CellularCoverageTilesets");
        }
    }
}
