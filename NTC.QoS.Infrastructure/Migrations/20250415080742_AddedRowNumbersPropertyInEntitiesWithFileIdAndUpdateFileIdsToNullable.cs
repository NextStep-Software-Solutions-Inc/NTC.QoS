using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedRowNumbersPropertyInEntitiesWithFileIdAndUpdateFileIdsToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars");

            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Files_FileId",
                table: "Speedtests");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "Speedtests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "Speedtests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "NAPs",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RowNumber",
                table: "Cellulars",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "Cellulars",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "Cells",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Files_FileId",
                table: "Speedtests",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars");

            migrationBuilder.DropForeignKey(
                name: "FK_Speedtests_Files_FileId",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "NAPs");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "Cells");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "Speedtests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RowNumber",
                table: "Cellulars",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "Cellulars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Speedtests_Files_FileId",
                table: "Speedtests",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
