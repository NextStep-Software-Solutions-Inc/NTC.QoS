using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedHasFilterDeletedInRolePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionId_RoleId_Active",
                table: "RolePermissions");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId_RoleId",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionId_RoleId",
                table: "RolePermissions");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId_RoleId_Active",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
