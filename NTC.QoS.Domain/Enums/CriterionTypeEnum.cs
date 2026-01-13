namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Defines the type of criterion used in rubric scoring
/// </summary>
public enum CriterionTypeEnum
{
    /// <summary>
    /// Fiber optic infrastructure coverage assessment
    /// </summary>
    FiberCoverage = 1,

    /// <summary>
    /// Internet access speed and reliability assessment
    /// </summary>
    InternetAccess = 2,

    /// <summary>
    /// Technology type availability (5G, 4G, LTE, etc.)
    /// </summary>
    TechnologyType = 3,

    /// <summary>
    /// Quality of service metrics (latency, signal strength, consistency)
    /// </summary>
    QualityOfService = 4,

    /// <summary>
    /// Overall coverage availability percentage
    /// </summary>
    CoverageAvailability = 5
}
