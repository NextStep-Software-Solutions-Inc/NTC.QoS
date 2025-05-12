using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCellAndCellularEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cells_Zone_ZoneId",
                table: "Cells");

            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Barangays_BarangayId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "CoorLatitude",
                table: "Cells");

            migrationBuilder.DropColumn(
                name: "CoorLongitude",
                table: "Cells");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarangayId",
                table: "Cellulars",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Cellulars",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ZoneId",
                table: "Cells",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Cells",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Cells",
                type: "geometry",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=3857;POINT (0 0)"));

            migrationBuilder.AddForeignKey(
                name: "FK_Cells_Zone_ZoneId",
                table: "Cells",
                column: "ZoneId",
                principalTable: "Zone",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Barangays_BarangayId",
                table: "Cellulars",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cells_Zone_ZoneId",
                table: "Cells");

            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Barangays_BarangayId",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Cells");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Cells");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarangayId",
                table: "Cellulars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "Cellulars",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ZoneId",
                table: "Cells",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoorLatitude",
                table: "Cells",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoorLongitude",
                table: "Cells",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Cells_Zone_ZoneId",
                table: "Cells",
                column: "ZoneId",
                principalTable: "Zone",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Barangays_BarangayId",
                table: "Cellulars",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
