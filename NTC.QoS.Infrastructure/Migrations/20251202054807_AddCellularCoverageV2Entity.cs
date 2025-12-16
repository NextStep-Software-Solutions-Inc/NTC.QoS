using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCellularCoverageV2Entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CellularCoveragesV2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CellSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry", nullable: false),
                    SignalStrength = table.Column<decimal>(type: "numeric", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsFileUploaded = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_CellularCoveragesV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                        column: x => x.CellSectorId,
                        principalTable: "CellSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_BandId",
                table: "CellularCoveragesV2",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_CellSectorId",
                table: "CellularCoveragesV2",
                column: "CellSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_MunicipalityId",
                table: "CellularCoveragesV2",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_NetworkId",
                table: "CellularCoveragesV2",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_TechnologyId",
                table: "CellularCoveragesV2",
                column: "TechnologyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CellularCoveragesV2");
        }
    }
}
