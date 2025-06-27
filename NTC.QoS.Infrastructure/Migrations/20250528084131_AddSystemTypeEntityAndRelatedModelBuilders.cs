using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemTypeEntityAndRelatedModelBuilders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemType",
                table: "Networks");

            migrationBuilder.AddColumn<Guid>(
                name: "SystemTypeId",
                table: "Networks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SystemTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_SystemTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemTypes_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Networks_SystemTypeId",
                table: "Networks",
                column: "SystemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemTypes_TechnologyId",
                table: "SystemTypes",
                column: "TechnologyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_SystemTypes_SystemTypeId",
                table: "Networks",
                column: "SystemTypeId",
                principalTable: "SystemTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_SystemTypes_SystemTypeId",
                table: "Networks");

            migrationBuilder.DropTable(
                name: "SystemTypes");

            migrationBuilder.DropIndex(
                name: "IX_Networks_SystemTypeId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "SystemTypeId",
                table: "Networks");

            migrationBuilder.AddColumn<int>(
                name: "SystemType",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
