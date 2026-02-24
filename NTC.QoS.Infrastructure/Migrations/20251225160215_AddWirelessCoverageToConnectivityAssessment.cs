using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWirelessCoverageToConnectivityAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WirelessCoverageJson",
                table: "ConnectivityAssessments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WirelessCoverageLastUpdated",
                table: "ConnectivityAssessments",
                type: "timestamp with time zone",
                nullable: true);

            // Create ReportTemplates table only if it doesn't exist
            // This handles dev environment where the table may already exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ReportTemplates"" (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""Description"" character varying(500) NOT NULL,
                    ""Instruction"" text NOT NULL,
                    ""IsActive"" boolean NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""CreatedBy"" text NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedBy"" text NOT NULL,
                    ""IsDeleted"" boolean NOT NULL,
                    ""DeletedAt"" timestamp with time zone,
                    ""DeletedBy"" text,
                    CONSTRAINT ""PK_ReportTemplates"" PRIMARY KEY (""Id"")
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ReportTemplates"";");

            migrationBuilder.DropColumn(
                name: "WirelessCoverageJson",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "WirelessCoverageLastUpdated",
                table: "ConnectivityAssessments");
        }
    }
}
