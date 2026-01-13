using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Infrastructure.Seed;

/// <summary>
/// Seeds the default CMTS v1.0.0 rubric configuration that matches the original hardcoded scoring logic.
/// Reference: ProcessConnectivityAssessmentJob.cs lines 2815-3200
/// </summary>
public static class RubricConfigurationSeeder
{
    // Predefined GUIDs for consistent seeding
    private static readonly Guid DefaultRubricId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid FiberCoverageCriterionId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid InternetAccessCriterionId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid TechnologyTypeCriterionId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid QualityOfServiceCriterionId = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static readonly Guid CoverageAvailabilityCriterionId = Guid.Parse("00000000-0000-0000-0000-000000000006");

    public static void SeedDefaultRubric(ModelBuilder modelBuilder)
    {
        var seedTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1. Seed RubricConfiguration
        var rubricConfig = new RubricConfiguration
        {
            Id = DefaultRubricId,
            Name = "CMTS Standard Rubric",
            Description = "Official CMTS Broadband Mapping Rubric - matches original hardcoded logic from ProcessConnectivityAssessmentJob",
            Version = "1.0.0",
            IsActive = true,
            IsDefault = true,
            EffectiveFrom = seedTime,
            EffectiveTo = null,
            ApplicableLocationType = null, // Applies to all location types
            ConfigurationJson = null, // Will be populated when rubric is loaded
            CreatedAt = seedTime,
            CreatedBy = "System",
            UpdatedAt = seedTime,
            UpdatedBy = "System",
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };

        modelBuilder.Entity<RubricConfiguration>().HasData(rubricConfig);

        // 2. Seed RubricCriteria (5 criteria)
        var criteria = new[]
        {
            new RubricCriterion
            {
                Id = FiberCoverageCriterionId,
                RubricConfigurationId = DefaultRubricId,
                CriterionType = CriterionTypeEnum.FiberCoverage,
                Name = "Fiber Coverage",
                Description = "Presence of fiber optic infrastructure and major telecommunications providers",
                MaxScore = 3,
                Weight = 1.0m,
                SortOrder = 1,
                IsEnabled = true,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricCriterion
            {
                Id = InternetAccessCriterionId,
                RubricConfigurationId = DefaultRubricId,
                CriterionType = CriterionTypeEnum.InternetAccess,
                Name = "Internet Access",
                Description = "Internet availability, speed, and reliability based on median download speeds",
                MaxScore = 3,
                Weight = 1.0m,
                SortOrder = 2,
                IsEnabled = true,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricCriterion
            {
                Id = TechnologyTypeCriterionId,
                RubricConfigurationId = DefaultRubricId,
                CriterionType = CriterionTypeEnum.TechnologyType,
                Name = "Technology Type",
                Description = "Broadband technology infrastructure presence and quality (5G, 4G/LTE, Fiber)",
                MaxScore = 3,
                Weight = 1.0m,
                SortOrder = 3,
                IsEnabled = true,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricCriterion
            {
                Id = QualityOfServiceCriterionId,
                RubricConfigurationId = DefaultRubricId,
                CriterionType = CriterionTypeEnum.QualityOfService,
                Name = "Quality of Service",
                Description = "Latency, signal strength, and service reliability metrics",
                MaxScore = 3,
                Weight = 1.0m,
                SortOrder = 4,
                IsEnabled = true,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricCriterion
            {
                Id = CoverageAvailabilityCriterionId,
                RubricConfigurationId = DefaultRubricId,
                CriterionType = CriterionTypeEnum.CoverageAvailability,
                Name = "Coverage Availability",
                Description = "Percentage of households/population with access to fixed or mobile broadband",
                MaxScore = 3,
                Weight = 1.0m,
                SortOrder = 5,
                IsEnabled = true,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        };

        modelBuilder.Entity<RubricCriterion>().HasData(criteria);

        // 3. Seed RubricThresholds for each criterion
        var thresholds = new List<RubricThreshold>();

        // ===== FIBER COVERAGE THRESHOLDS =====
        // Reference: CalculateFiberCoverageScore() - Lines 2815-2872
        thresholds.AddRange(new[]
        {
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                RubricCriterionId = FiberCoverageCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.GreaterThanOrEqual,
                PrimaryValue = 2.0m,
                SecondaryValue = null,
                MetricType = "MajorTelcoCount",
                ConditionLogic = null,
                Description = "Served: ≥2 major telcos (Globe, Smart, DITO) with fiber infrastructure",
                SortOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                RubricCriterionId = FiberCoverageCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.Equals,
                PrimaryValue = 1.0m,
                SecondaryValue = null,
                MetricType = "MajorTelcoCount",
                ConditionLogic = null,
                Description = "Underserved: Only 1 major telco with fiber",
                SortOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
                RubricCriterionId = FiberCoverageCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.GreaterThan,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "LocalVasProviderCount",
                ConditionLogic = "{\"conditions\":[{\"field\":\"MajorTelcoCount\",\"operator\":\"Equals\",\"value\":0}]}",
                Description = "Underserved: No major telcos but local/VAS providers have fiber",
                SortOrder = 3,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000013"),
                RubricCriterionId = FiberCoverageCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.Equals,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "FiberNAPCount",
                ConditionLogic = null,
                Description = "Unserved: No fiber infrastructure at all",
                SortOrder = 4,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        });

        // ===== INTERNET ACCESS THRESHOLDS =====
        // Reference: CalculateInternetAccessScore() - Lines 2905-2960
        thresholds.AddRange(new[]
        {
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000020"),
                RubricCriterionId = InternetAccessCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.GreaterThanOrEqual,
                PrimaryValue = 25.0m,
                SecondaryValue = null,
                MetricType = "MedianDownloadSpeedMbps",
                ConditionLogic = "{\"conditions\":[{\"field\":\"SpeedVariability\",\"operator\":\"LessThanOrEqual\",\"value\":0.3}]}",
                Description = "Served: ≥25 Mbps median download speed AND reliable (≤30% variability)",
                SortOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000021"),
                RubricCriterionId = InternetAccessCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.GreaterThanOrEqual,
                PrimaryValue = 25.0m,
                SecondaryValue = null,
                MetricType = "MedianDownloadSpeedMbps",
                ConditionLogic = "{\"conditions\":[{\"field\":\"SpeedVariability\",\"operator\":\"GreaterThan\",\"value\":0.3}]}",
                Description = "Underserved: ≥25 Mbps but unreliable (>30% variability)",
                SortOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000022"),
                RubricCriterionId = InternetAccessCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.Between,
                PrimaryValue = 10.0m,
                SecondaryValue = 24.99m,
                MetricType = "MedianDownloadSpeedMbps",
                ConditionLogic = null,
                Description = "Underserved: 10-25 Mbps (below national broadband standards)",
                SortOrder = 3,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000023"),
                RubricCriterionId = InternetAccessCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.LessThan,
                PrimaryValue = 10.0m,
                SecondaryValue = null,
                MetricType = "MedianDownloadSpeedMbps",
                ConditionLogic = null,
                Description = "Unserved: <10 Mbps (minimal/no practical broadband access)",
                SortOrder = 4,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000024"),
                RubricCriterionId = InternetAccessCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.LessThan,
                PrimaryValue = 3.0m,
                SecondaryValue = null,
                MetricType = "SpeedTestCount",
                ConditionLogic = null,
                Description = "Unserved: Insufficient data (<3 speed tests)",
                SortOrder = 5,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        });

        // ===== TECHNOLOGY TYPE THRESHOLDS =====
        // Reference: CalculateTechnologyTypeScore() - Lines 2967-3042
        thresholds.AddRange(new[]
        {
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000030"),
                RubricCriterionId = TechnologyTypeCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.GreaterThan,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "FiberNAPCount",
                ConditionLogic = null,
                Description = "Served: Fiber optic infrastructure present",
                SortOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000031"),
                RubricCriterionId = TechnologyTypeCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.GreaterThan,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "Count5G",
                ConditionLogic = null,
                Description = "Served: 5G infrastructure in place and active",
                SortOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000032"),
                RubricCriterionId = TechnologyTypeCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.GreaterThan,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "Count4G",
                ConditionLogic = "{\"conditions\":[{\"field\":\"IsLegacyDominated\",\"operator\":\"Equals\",\"value\":false}]}",
                Description = "Served: 4G/LTE infrastructure present and not legacy-dominated",
                SortOrder = 3,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000033"),
                RubricCriterionId = TechnologyTypeCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.Equals,
                PrimaryValue = 1.0m,
                SecondaryValue = null,
                MetricType = "IsLegacyDominated",
                ConditionLogic = "{\"conditions\":[{\"field\":\"HasModernTechnology\",\"operator\":\"Equals\",\"value\":true}]}",
                Description = "Underserved: Has modern tech but legacy (2G/3G) dominates",
                SortOrder = 4,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000034"),
                RubricCriterionId = TechnologyTypeCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.GreaterThan,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "Count3G",
                ConditionLogic = null,
                Description = "Underserved: Only legacy 3G technology available",
                SortOrder = 5,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000035"),
                RubricCriterionId = TechnologyTypeCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.Equals,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "TotalTechnologyCount",
                ConditionLogic = null,
                Description = "Unserved: No identifiable broadband technology",
                SortOrder = 6,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        });

        // ===== QUALITY OF SERVICE THRESHOLDS =====
        // Reference: CalculateQualityOfServiceScore() - Lines 3049-3120
        thresholds.AddRange(new[]
        {
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000040"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.LessThan,
                PrimaryValue = 50.0m,
                SecondaryValue = null,
                MetricType = "AverageLatencyMs",
                ConditionLogic = "{\"conditions\":[{\"field\":\"AverageSignalStrength\",\"operator\":\"GreaterThanOrEqual\",\"value\":4.0},{\"field\":\"MeetsMinimumSpeedRequirement\",\"operator\":\"Equals\",\"value\":true}]}",
                Description = "Served: Latency <50ms AND good signal (≥4.0) AND consistent speeds",
                SortOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000041"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 3, // Served - Exceptional 5G
                Operator = ThresholdOperatorEnum.LessThanOrEqual,
                PrimaryValue = 20.0m,
                SecondaryValue = null,
                MetricType = "AverageLatencyMs",
                ConditionLogic = "{\"conditions\":[{\"field\":\"Count5G\",\"operator\":\"GreaterThan\",\"value\":0},{\"field\":\"AverageDownloadSpeedMbps\",\"operator\":\"GreaterThanOrEqual\",\"value\":100},{\"field\":\"AverageSignalStrength\",\"operator\":\"GreaterThanOrEqual\",\"value\":4.5}]}",
                Description = "Served: Exceptional 5G quality (≤20ms, ≥100 Mbps, signal ≥4.5)",
                SortOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000042"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.Between,
                PrimaryValue = 50.0m,
                SecondaryValue = 100.0m,
                MetricType = "AverageLatencyMs",
                ConditionLogic = null,
                Description = "Underserved: Moderate latency (50-100ms)",
                SortOrder = 3,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000043"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.Between,
                PrimaryValue = 2.0m,
                SecondaryValue = 3.99m,
                MetricType = "AverageSignalStrength",
                ConditionLogic = null,
                Description = "Underserved: Moderate signal strength (2.0-3.99)",
                SortOrder = 4,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000044"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.GreaterThan,
                PrimaryValue = 100.0m,
                SecondaryValue = null,
                MetricType = "AverageLatencyMs",
                ConditionLogic = null,
                Description = "Unserved: High latency (>100ms) - unusable for essential tasks",
                SortOrder = 5,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000045"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.LessThanOrEqual,
                PrimaryValue = 2.0m,
                SecondaryValue = null,
                MetricType = "AverageSignalStrength",
                ConditionLogic = null,
                Description = "Unserved: Poor signal strength (≤2.0)",
                SortOrder = 6,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000046"),
                RubricCriterionId = QualityOfServiceCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.LessThan,
                PrimaryValue = 3.0m,
                SecondaryValue = null,
                MetricType = "SpeedTestCount",
                ConditionLogic = null,
                Description = "Unserved: Insufficient data (<3 speed tests)",
                SortOrder = 7,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        });

        // ===== COVERAGE AVAILABILITY THRESHOLDS =====
        // Reference: CalculateCoverageAvailabilityScore() - Lines 3127-3211
        thresholds.AddRange(new[]
        {
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000050"),
                RubricCriterionId = CoverageAvailabilityCriterionId,
                ScoreValue = 3, // Served
                Operator = ThresholdOperatorEnum.GreaterThanOrEqual,
                PrimaryValue = 100.0m,
                SecondaryValue = null,
                MetricType = "EstimatedCoveragePercent",
                ConditionLogic = null,
                Description = "Served: 100% of households/population have access",
                SortOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000051"),
                RubricCriterionId = CoverageAvailabilityCriterionId,
                ScoreValue = 2, // Underserved
                Operator = ThresholdOperatorEnum.Between,
                PrimaryValue = 40.0m,
                SecondaryValue = 99.99m,
                MetricType = "EstimatedCoveragePercent",
                ConditionLogic = null,
                Description = "Underserved: 40-99% of population has access",
                SortOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000052"),
                RubricCriterionId = CoverageAvailabilityCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.LessThan,
                PrimaryValue = 40.0m,
                SecondaryValue = null,
                MetricType = "EstimatedCoveragePercent",
                ConditionLogic = null,
                Description = "Unserved: <40% have access; infrastructure is sparse",
                SortOrder = 3,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricThreshold
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000053"),
                RubricCriterionId = CoverageAvailabilityCriterionId,
                ScoreValue = 1, // Unserved
                Operator = ThresholdOperatorEnum.Equals,
                PrimaryValue = 0.0m,
                SecondaryValue = null,
                MetricType = "TotalAssets",
                ConditionLogic = null,
                Description = "Unserved: No infrastructure (no coverage)",
                SortOrder = 4,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        });

        modelBuilder.Entity<RubricThreshold>().HasData(thresholds);

        // 4. Seed RubricStatusRanges
        // Reference: DetermineConnectivityStatus() - Line 3656
        var statusRanges = new[]
        {
            new RubricStatusRange
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000060"),
                RubricConfigurationId = DefaultRubricId,
                StatusName = "Served",
                MinScore = 11,
                MaxScore = 15,
                StatusValue = (int)ConnectivityStatusEnum.Served,
                DisplayColor = "#28A745",
                Description = "Fully developed, meets most technical and social benchmarks",
                SortOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricStatusRange
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000061"),
                RubricConfigurationId = DefaultRubricId,
                StatusName = "Underserved",
                MinScore = 6,
                MaxScore = 10,
                StatusValue = (int)ConnectivityStatusEnum.Underserved,
                DisplayColor = "#FFC107",
                Description = "Has basic infrastructure or partial access, but lacks quality, affordability, or consistency",
                SortOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            },
            new RubricStatusRange
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000062"),
                RubricConfigurationId = DefaultRubricId,
                StatusName = "Unserved",
                MinScore = 0,
                MaxScore = 5,
                StatusValue = (int)ConnectivityStatusEnum.Unserved,
                DisplayColor = "#DC3545",
                Description = "Virtually no broadband access or supporting infrastructure",
                SortOrder = 3,
                CreatedAt = seedTime,
                CreatedBy = "System",
                UpdatedAt = seedTime,
                UpdatedBy = "System",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            }
        };

        modelBuilder.Entity<RubricStatusRange>().HasData(statusRanges);
    }
}
