using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicRubricSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RubricConfigurationId",
                table: "ConnectivityAssessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RubricVersion",
                table: "ConnectivityAssessments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RubricConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApplicableLocationType = table.Column<int>(type: "integer", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_RubricConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RubricCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_RubricCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricCriteria_RubricConfigurations_RubricConfigurationId",
                        column: x => x.RubricConfigurationId,
                        principalTable: "RubricConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricStatusRanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinScore = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    StatusValue = table.Column<int>(type: "integer", nullable: false),
                    DisplayColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_RubricStatusRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricStatusRanges_RubricConfigurations_RubricConfiguration~",
                        column: x => x.RubricConfigurationId,
                        principalTable: "RubricConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricCriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoreValue = table.Column<int>(type: "integer", nullable: false),
                    Operator = table.Column<int>(type: "integer", nullable: false),
                    PrimaryValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SecondaryValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MetricType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConditionLogic = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_RubricThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricThresholds_RubricCriteria_RubricCriterionId",
                        column: x => x.RubricCriterionId,
                        principalTable: "RubricCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectivityAssessments_RubricConfigurationId",
                table: "ConnectivityAssessments",
                column: "RubricConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricConfigurations_EffectiveFrom",
                table: "RubricConfigurations",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_RubricConfigurations_IsActive",
                table: "RubricConfigurations",
                column: "IsActive",
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RubricConfigurations_IsDefault",
                table: "RubricConfigurations",
                column: "IsDefault",
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RubricConfigurations_Version_IsDeleted",
                table: "RubricConfigurations",
                columns: new[] { "Version", "IsDeleted" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RubricCriteria_CriterionType",
                table: "RubricCriteria",
                column: "CriterionType");

            migrationBuilder.CreateIndex(
                name: "IX_RubricCriteria_RubricConfigurationId",
                table: "RubricCriteria",
                column: "RubricConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricStatusRanges_RubricConfigurationId",
                table: "RubricStatusRanges",
                column: "RubricConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricThresholds_RubricCriterionId",
                table: "RubricThresholds",
                column: "RubricCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricThresholds_ScoreValue",
                table: "RubricThresholds",
                column: "ScoreValue");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectivityAssessments_RubricConfigurations_RubricConfigur~",
                table: "ConnectivityAssessments",
                column: "RubricConfigurationId",
                principalTable: "RubricConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectivityAssessments_RubricConfigurations_RubricConfigur~",
                table: "ConnectivityAssessments");

            migrationBuilder.DropTable(
                name: "RubricStatusRanges");

            migrationBuilder.DropTable(
                name: "RubricThresholds");

            migrationBuilder.DropTable(
                name: "RubricCriteria");

            migrationBuilder.DropTable(
                name: "RubricConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_ConnectivityAssessments_RubricConfigurationId",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "RubricConfigurationId",
                table: "ConnectivityAssessments");

            migrationBuilder.DropColumn(
                name: "RubricVersion",
                table: "ConnectivityAssessments");
        }
    }
}
