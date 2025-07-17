using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHasFilterIsDeletedFalseInTechnology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Technologies_Standard",
                table: "Technologies");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Standard",
                table: "Technologies",
                column: "Standard",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Technologies_Standard",
                table: "Technologies");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Standard",
                table: "Technologies",
                column: "Standard",
                unique: true);
        }
    }
}
