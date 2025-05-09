using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCellSectorEntityAndModelBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sectors_Cells_CellId",
                table: "Sectors");

            migrationBuilder.RenameColumn(
                name: "CellId",
                table: "Sectors",
                newName: "CellSectorId");

            migrationBuilder.RenameIndex(
                name: "IX_Sectors_CellId",
                table: "Sectors",
                newName: "IX_Sectors_CellSectorId");

            migrationBuilder.CreateTable(
                name: "CellSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CellId = table.Column<Guid>(type: "uuid", nullable: false),
                    AntennaType = table.Column<int>(type: "integer", nullable: false),
                    Azimuth = table.Column<decimal>(type: "numeric", nullable: false),
                    BeamTilt = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_CellSectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellSectors_Cells_CellId",
                        column: x => x.CellId,
                        principalTable: "Cells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CellSectors_CellId",
                table: "CellSectors",
                column: "CellId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sectors_CellSectors_CellSectorId",
                table: "Sectors",
                column: "CellSectorId",
                principalTable: "CellSectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sectors_CellSectors_CellSectorId",
                table: "Sectors");

            migrationBuilder.DropTable(
                name: "CellSectors");

            migrationBuilder.RenameColumn(
                name: "CellSectorId",
                table: "Sectors",
                newName: "CellId");

            migrationBuilder.RenameIndex(
                name: "IX_Sectors_CellSectorId",
                table: "Sectors",
                newName: "IX_Sectors_CellId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sectors_Cells_CellId",
                table: "Sectors",
                column: "CellId",
                principalTable: "Cells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
