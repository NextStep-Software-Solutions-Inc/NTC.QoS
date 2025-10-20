namespace NTC.QoS.Domain.Enums;

/// <summary>
/// CMTS Broadband Mapping Rubric connectivity classification
/// </summary>
public enum ConnectivityStatusEnum
{
    /// <summary>
    /// Total score = 11-15 points
    /// Fully developed, meets most technical and social benchmarks
    /// </summary>
    Served = 3,

    /// <summary>
    /// Total score = 6-10 points  
    /// Has basic infrastructure or partial access, but lacks quality, affordability, or consistency
    /// </summary>
    Underserved = 2,

    /// <summary>
    /// Total score ? 5 points
    /// Virtually no broadband access or supporting infrastructure
    /// </summary>
    Unserved = 1
}