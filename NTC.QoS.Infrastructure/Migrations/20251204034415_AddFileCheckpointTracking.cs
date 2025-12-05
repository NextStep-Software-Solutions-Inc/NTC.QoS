using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileCheckpointTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckpointTime",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastProcessedBatchNumber",
                table: "Files",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastProcessedRowNumber",
                table: "Files",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingJobId",
                table: "Files",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckpointTime",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "LastProcessedBatchNumber",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "LastProcessedRowNumber",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ProcessingJobId",
                table: "Files");
        }
    }
}
