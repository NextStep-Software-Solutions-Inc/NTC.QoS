using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWirelessValidationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WirelessValidations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BarangayId = table.Column<Guid>(type: "uuid", nullable: true),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    RSSI = table.Column<decimal>(type: "numeric", nullable: false),
                    IsMarginal = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_WirelessValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WirelessValidations_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_BarangayId",
                table: "WirelessValidations",
                column: "BarangayId");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_IsMarginal",
                table: "WirelessValidations",
                column: "IsMarginal");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_ServiceProviderId",
                table: "WirelessValidations",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidation_TechnologyId_BandId",
                table: "WirelessValidations",
                columns: new[] { "TechnologyId", "BandId" });

            migrationBuilder.CreateIndex(
                name: "IX_WirelessValidations_BandId",
                table: "WirelessValidations",
                column: "BandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WirelessValidations");
        }
    }
}
