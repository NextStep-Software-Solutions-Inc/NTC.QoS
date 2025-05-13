using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointLocationRemoveBarangay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Barangays_BarangayId",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "CoorLatitude",
                table: "Speedtests");

            migrationBuilder.RenameColumn(
                name: "CoorLongitude",
                table: "Speedtests",
                newName: "Address");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarangayId",
                table: "Speedtests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Speedtests",
                type: "geometry",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=3857;POINT (0 0)"));

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Barangays_BarangayId",
                table: "Speedtests",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Barangays_BarangayId",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Speedtests");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Speedtests",
                newName: "CoorLongitude");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarangayId",
                table: "Speedtests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoorLatitude",
                table: "Speedtests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Barangays_BarangayId",
                table: "Speedtests",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
