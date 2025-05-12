using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToNetTopologuSuiteForPostGIS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoorLatitude",
                table: "NAPs");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "NAPs");

            migrationBuilder.RenameColumn(
                name: "CoorLongitude",
                table: "NAPs",
                newName: "Address");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<bool>(
                name: "IsFileUploaded",
                table: "NAPs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "NAPs",
                type: "geometry",
                nullable: false,
                defaultValue: (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (0 0)"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFileUploaded",
                table: "NAPs");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "NAPs");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "NAPs",
                newName: "CoorLongitude");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<string>(
                name: "CoorLatitude",
                table: "NAPs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "NAPs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "Files",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
