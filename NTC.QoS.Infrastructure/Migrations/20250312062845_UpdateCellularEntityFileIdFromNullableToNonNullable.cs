using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCellularEntityFileIdFromNullableToNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "Cellulars",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Cellulars_Files_FileId",
                table: "Cellulars",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");
        }
    }
}
