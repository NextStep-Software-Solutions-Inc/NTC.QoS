using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProviderLogoToBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogoPath",
                table: "Providers",
                newName: "LogoFileName");

            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Providers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "Providers",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "Providers");

            migrationBuilder.RenameColumn(
                name: "LogoFileName",
                table: "Providers",
                newName: "LogoPath");
        }
    }
}
