using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Zone",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "UserTypes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Technologies",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "StudyNetworks",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "StudyMunicipalities",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Studies",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Speedtests",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Services",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "ServiceProviders",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Sectors",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Regions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Provinces",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Providers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "NetworkTemplates",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Networks",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "NAPs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Municipalities",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "MIMOs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Links",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Generations",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "GenerationBands",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Files",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Cellulars",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Cells",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Barangays",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Bands",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "AreaStudies",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "AntennaPatterns",
                newName: "UpdatedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Zone",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Zone",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "UserTypes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UserTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Technologies",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Technologies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "StudyNetworks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StudyNetworks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "StudyMunicipalities",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StudyMunicipalities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Studies",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Studies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Speedtests",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Speedtests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Services",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceProviders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ServiceProviders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Sectors",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Sectors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Regions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Regions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Provinces",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Provinces",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Providers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Providers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "NetworkTemplates",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "NetworkTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Networks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Networks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "NAPs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "NAPs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Municipalities",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Municipalities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "MIMOs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MIMOs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Links",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Links",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Generations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Generations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "GenerationBands",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "GenerationBands",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Cellulars",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Cellulars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Cells",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Cells",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Barangays",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Barangays",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Bands",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Bands",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "AreaStudies",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AreaStudies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "AntennaPatterns",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AntennaPatterns",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Zone");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UserTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudyNetworks");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudyMunicipalities");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Studies");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Speedtests");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "NetworkTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "NAPs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MIMOs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Generations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "GenerationBands");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Cellulars");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Cells");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Bands");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AreaStudies");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AntennaPatterns");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Zone",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "UserTypes",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Users",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Technologies",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "StudyNetworks",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "StudyMunicipalities",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Studies",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Speedtests",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Services",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ServiceProviders",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Sectors",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Regions",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Provinces",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Providers",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "NetworkTemplates",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Networks",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "NAPs",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Municipalities",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "MIMOs",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Links",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Generations",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "GenerationBands",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Files",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Cellulars",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Cells",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Barangays",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Bands",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "AreaStudies",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "AntennaPatterns",
                newName: "UploadedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Zone",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "UserTypes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Technologies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "StudyNetworks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "StudyMunicipalities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Studies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Speedtests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Services",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceProviders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Sectors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Regions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Provinces",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Providers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "NetworkTemplates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Networks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "NAPs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Municipalities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "MIMOs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Links",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Generations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "GenerationBands",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Files",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Cellulars",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Cells",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Barangays",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "Bands",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "AreaStudies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "AntennaPatterns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
