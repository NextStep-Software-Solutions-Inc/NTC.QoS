using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCellularCoverageV2Navigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Bands_BandId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Networks_NetworkId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoveragesV2_NetworkId",
                table: "CellularCoveragesV2");

            migrationBuilder.RenameIndex(
                name: "IX_CellularCoveragesV2_MunicipalityId",
                table: "CellularCoveragesV2",
                newName: "IX_CellularCoverageV2_MunicipalityId");

            migrationBuilder.RenameIndex(
                name: "IX_CellularCoveragesV2_CellSectorId",
                table: "CellularCoveragesV2",
                newName: "IX_CellularCoverageV2_CellSectorId");

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "CellularCoveragesV2",
                type: "geometry (Point,4326)",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"),
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageV2_Location",
                table: "CellularCoveragesV2",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoverageV2_Network_Band_Municipality",
                table: "CellularCoveragesV2",
                columns: new[] { "NetworkId", "BandId", "MunicipalityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Bands_BandId",
                table: "CellularCoveragesV2",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                table: "CellularCoveragesV2",
                column: "CellSectorId",
                principalTable: "CellSectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                table: "CellularCoveragesV2",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Networks_NetworkId",
                table: "CellularCoveragesV2",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                table: "CellularCoveragesV2",
                column: "TechnologyId",
                principalTable: "Technologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Bands_BandId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Networks_NetworkId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverageV2_Location",
                table: "CellularCoveragesV2");

            migrationBuilder.DropIndex(
                name: "IX_CellularCoverageV2_Network_Band_Municipality",
                table: "CellularCoveragesV2");

            migrationBuilder.RenameIndex(
                name: "IX_CellularCoverageV2_MunicipalityId",
                table: "CellularCoveragesV2",
                newName: "IX_CellularCoveragesV2_MunicipalityId");

            migrationBuilder.RenameIndex(
                name: "IX_CellularCoverageV2_CellSectorId",
                table: "CellularCoveragesV2",
                newName: "IX_CellularCoveragesV2_CellSectorId");

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "CellularCoveragesV2",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry (Point,4326)",
                oldDefaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));

            migrationBuilder.CreateIndex(
                name: "IX_CellularCoveragesV2_NetworkId",
                table: "CellularCoveragesV2",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Bands_BandId",
                table: "CellularCoveragesV2",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_CellSectors_CellSectorId",
                table: "CellularCoveragesV2",
                column: "CellSectorId",
                principalTable: "CellSectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Municipalities_MunicipalityId",
                table: "CellularCoveragesV2",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Networks_NetworkId",
                table: "CellularCoveragesV2",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CellularCoveragesV2_Technologies_TechnologyId",
                table: "CellularCoveragesV2",
                column: "TechnologyId",
                principalTable: "Technologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
