using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestrictOrganizationDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Key",
                table: "Roles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            // Remove duplicate organizations before creating unique index
            migrationBuilder.Sql(@"
                DELETE FROM ""Organizations"" a USING (
                    SELECT MIN(ctid) as ctid, ""Name""
                    FROM ""Organizations"" 
                    WHERE ""IsDeleted"" = FALSE
                    GROUP BY ""Name"" HAVING COUNT(*) > 1
                ) b
                WHERE a.""Name"" = b.""Name"" AND a.ctid <> b.ctid;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Key",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Key",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Name",
                table: "Organizations");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
