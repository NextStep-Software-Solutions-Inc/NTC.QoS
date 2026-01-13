using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultRubricConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RubricConfigurations",
                columns: new[] { "Id", "ApplicableLocationType", "ConfigurationJson", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "EffectiveFrom", "EffectiveTo", "IsActive", "IsDefault", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Official CMTS Broadband Mapping Rubric - matches original hardcoded logic from ProcessConnectivityAssessmentJob", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, false, "CMTS Standard Rubric", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", "1.0.0" });

            migrationBuilder.InsertData(
                table: "RubricCriteria",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "CriterionType", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "IsEnabled", "MaxScore", "Name", "RubricConfigurationId", "SortOrder", "UpdatedAt", "UpdatedBy", "Weight" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1, null, null, "Presence of fiber optic infrastructure and major telecommunications providers", false, true, 3, "Fiber Coverage", new Guid("00000000-0000-0000-0000-000000000001"), 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1.0m },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 2, null, null, "Internet availability, speed, and reliability based on median download speeds", false, true, 3, "Internet Access", new Guid("00000000-0000-0000-0000-000000000001"), 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1.0m },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 3, null, null, "Broadband technology infrastructure presence and quality (5G, 4G/LTE, Fiber)", false, true, 3, "Technology Type", new Guid("00000000-0000-0000-0000-000000000001"), 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1.0m },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 4, null, null, "Latency, signal strength, and service reliability metrics", false, true, 3, "Quality of Service", new Guid("00000000-0000-0000-0000-000000000001"), 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1.0m },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 5, null, null, "Percentage of households/population with access to fixed or mobile broadband", false, true, 3, "Coverage Availability", new Guid("00000000-0000-0000-0000-000000000001"), 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1.0m }
                });

            migrationBuilder.InsertData(
                table: "RubricStatusRanges",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DisplayColor", "IsDeleted", "MaxScore", "MinScore", "RubricConfigurationId", "SortOrder", "StatusName", "StatusValue", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000060"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Fully developed, meets most technical and social benchmarks", "#28A745", false, 15, 11, new Guid("00000000-0000-0000-0000-000000000001"), 1, "Served", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000061"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Has basic infrastructure or partial access, but lacks quality, affordability, or consistency", "#FFC107", false, 10, 6, new Guid("00000000-0000-0000-0000-000000000001"), 2, "Underserved", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000062"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Virtually no broadband access or supporting infrastructure", "#DC3545", false, 5, 0, new Guid("00000000-0000-0000-0000-000000000001"), 3, "Unserved", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" }
                });

            migrationBuilder.InsertData(
                table: "RubricThresholds",
                columns: new[] { "Id", "ConditionLogic", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "MetricType", "Operator", "PrimaryValue", "RubricCriterionId", "ScoreValue", "SecondaryValue", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000010"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: ≥2 major telcos (Globe, Smart, DITO) with fiber infrastructure", false, "MajorTelcoCount", 1, 2.0m, new Guid("00000000-0000-0000-0000-000000000002"), 3, null, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: Only 1 major telco with fiber", false, "MajorTelcoCount", 4, 1.0m, new Guid("00000000-0000-0000-0000-000000000002"), 2, null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), "{\"conditions\":[{\"field\":\"MajorTelcoCount\",\"operator\":\"Equals\",\"value\":0}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: No major telcos but local/VAS providers have fiber", false, "LocalVasProviderCount", 6, 0.0m, new Guid("00000000-0000-0000-0000-000000000002"), 2, null, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: No fiber infrastructure at all", false, "FiberNAPCount", 4, 0.0m, new Guid("00000000-0000-0000-0000-000000000002"), 1, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000020"), "{\"conditions\":[{\"field\":\"SpeedVariability\",\"operator\":\"LessThanOrEqual\",\"value\":0.3}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: ≥25 Mbps median download speed AND reliable (≤30% variability)", false, "MedianDownloadSpeedMbps", 1, 25.0m, new Guid("00000000-0000-0000-0000-000000000003"), 3, null, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), "{\"conditions\":[{\"field\":\"SpeedVariability\",\"operator\":\"GreaterThan\",\"value\":0.3}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: ≥25 Mbps but unreliable (>30% variability)", false, "MedianDownloadSpeedMbps", 1, 25.0m, new Guid("00000000-0000-0000-0000-000000000003"), 2, null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: 10-25 Mbps (below national broadband standards)", false, "MedianDownloadSpeedMbps", 3, 10.0m, new Guid("00000000-0000-0000-0000-000000000003"), 2, 24.99m, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000023"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: <10 Mbps (minimal/no practical broadband access)", false, "MedianDownloadSpeedMbps", 2, 10.0m, new Guid("00000000-0000-0000-0000-000000000003"), 1, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000024"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: Insufficient data (<3 speed tests)", false, "SpeedTestCount", 2, 3.0m, new Guid("00000000-0000-0000-0000-000000000003"), 1, null, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000030"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: Fiber optic infrastructure present", false, "FiberNAPCount", 6, 0.0m, new Guid("00000000-0000-0000-0000-000000000004"), 3, null, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000031"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: 5G infrastructure in place and active", false, "Count5G", 6, 0.0m, new Guid("00000000-0000-0000-0000-000000000004"), 3, null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000032"), "{\"conditions\":[{\"field\":\"IsLegacyDominated\",\"operator\":\"Equals\",\"value\":false}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: 4G/LTE infrastructure present and not legacy-dominated", false, "Count4G", 6, 0.0m, new Guid("00000000-0000-0000-0000-000000000004"), 3, null, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000033"), "{\"conditions\":[{\"field\":\"HasModernTechnology\",\"operator\":\"Equals\",\"value\":true}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: Has modern tech but legacy (2G/3G) dominates", false, "IsLegacyDominated", 4, 1.0m, new Guid("00000000-0000-0000-0000-000000000004"), 2, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000034"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: Only legacy 3G technology available", false, "Count3G", 6, 0.0m, new Guid("00000000-0000-0000-0000-000000000004"), 2, null, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000035"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: No identifiable broadband technology", false, "TotalTechnologyCount", 4, 0.0m, new Guid("00000000-0000-0000-0000-000000000004"), 1, null, 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000040"), "{\"conditions\":[{\"field\":\"AverageSignalStrength\",\"operator\":\"GreaterThanOrEqual\",\"value\":4.0},{\"field\":\"MeetsMinimumSpeedRequirement\",\"operator\":\"Equals\",\"value\":true}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: Latency <50ms AND good signal (≥4.0) AND consistent speeds", false, "AverageLatencyMs", 2, 50.0m, new Guid("00000000-0000-0000-0000-000000000005"), 3, null, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000041"), "{\"conditions\":[{\"field\":\"Count5G\",\"operator\":\"GreaterThan\",\"value\":0},{\"field\":\"AverageDownloadSpeedMbps\",\"operator\":\"GreaterThanOrEqual\",\"value\":100},{\"field\":\"AverageSignalStrength\",\"operator\":\"GreaterThanOrEqual\",\"value\":4.5}]}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: Exceptional 5G quality (≤20ms, ≥100 Mbps, signal ≥4.5)", false, "AverageLatencyMs", 7, 20.0m, new Guid("00000000-0000-0000-0000-000000000005"), 3, null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000042"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: Moderate latency (50-100ms)", false, "AverageLatencyMs", 3, 50.0m, new Guid("00000000-0000-0000-0000-000000000005"), 2, 100.0m, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000043"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: Moderate signal strength (2.0-3.99)", false, "AverageSignalStrength", 3, 2.0m, new Guid("00000000-0000-0000-0000-000000000005"), 2, 3.99m, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000044"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: High latency (>100ms) - unusable for essential tasks", false, "AverageLatencyMs", 6, 100.0m, new Guid("00000000-0000-0000-0000-000000000005"), 1, null, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000045"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: Poor signal strength (≤2.0)", false, "AverageSignalStrength", 7, 2.0m, new Guid("00000000-0000-0000-0000-000000000005"), 1, null, 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000046"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: Insufficient data (<3 speed tests)", false, "SpeedTestCount", 2, 3.0m, new Guid("00000000-0000-0000-0000-000000000005"), 1, null, 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000050"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Served: 100% of households/population have access", false, "EstimatedCoveragePercent", 1, 100.0m, new Guid("00000000-0000-0000-0000-000000000006"), 3, null, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000051"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Underserved: 40-99% of population has access", false, "EstimatedCoveragePercent", 3, 40.0m, new Guid("00000000-0000-0000-0000-000000000006"), 2, 99.99m, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000052"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: <40% have access; infrastructure is sparse", false, "EstimatedCoveragePercent", 2, 40.0m, new Guid("00000000-0000-0000-0000-000000000006"), 1, null, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { new Guid("00000000-0000-0000-0000-000000000053"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, "Unserved: No infrastructure (no coverage)", false, "TotalAssets", 4, 0.0m, new Guid("00000000-0000-0000-0000-000000000006"), 1, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RubricStatusRanges",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000060"));

            migrationBuilder.DeleteData(
                table: "RubricStatusRanges",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000061"));

            migrationBuilder.DeleteData(
                table: "RubricStatusRanges",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000062"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000046"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000051"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                table: "RubricThresholds",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000053"));

            migrationBuilder.DeleteData(
                table: "RubricCriteria",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "RubricCriteria",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "RubricCriteria",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "RubricCriteria",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "RubricCriteria",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "RubricConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
