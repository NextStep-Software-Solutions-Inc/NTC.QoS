using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectivityAssessmentProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectivityAssessmentProvider",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectivityAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    CellTowerCount = table.Column<int>(type: "integer", nullable: false),
                    FiberNAPCount = table.Column<int>(type: "integer", nullable: false),
                    WiFiHotspotCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAssetCount = table.Column<int>(type: "integer", nullable: false),
                    AssetPercentage = table.Column<double>(type: "double precision", nullable: false),
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
                    table.PrimaryKey("PK_ConnectivityAssessmentProvider", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectivityAssessmentProvider_ConnectivityAssessments_Conn~",
                        column: x => x.ConnectivityAssessmentId,
                        principalTable: "ConnectivityAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessmentProvider_ConnectivityAssessmentId",
                table: "ConnectivityAssessmentProvider",
                column: "ConnectivityAssessmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectivityAssessmentProvider");
        }
    }
}
