using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectivityAssessmentScheduler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectivityAssessmentSchedulers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HangfireJobId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LocationType = table.Column<int>(type: "integer", nullable: false),
                    LocationIdsJson = table.Column<string>(type: "text", nullable: false),
                    LastExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SendNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationEmailsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectivityAssessmentSchedulers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectivityAssessmentSchedulers");
        }
    }
}
