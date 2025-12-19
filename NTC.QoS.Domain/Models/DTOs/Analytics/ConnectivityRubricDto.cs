namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// CMTS Broadband Mapping Rubric information containing connectivity status definitions and scoring criteria.
/// </summary>
public class ConnectivityRubricDto
{
    public List<ConnectivityStatusDto> ConnectivityStatuses { get; set; } = new();
    public List<RubricCriterionDto> Criteria { get; set; } = new();
    public ScoringGuideDto ScoringGuide { get; set; } = new();
}

/// <summary>
/// Defines a connectivity status level (Served, Underserved, Unserved) with its characteristics.
/// </summary>
public class ConnectivityStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string ScoreRange { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Characteristics { get; set; } = new();
}

/// <summary>
/// Represents a single criterion in the CMTS rubric with scoring definitions.
/// </summary>
public class RubricCriterionDto
{
    public string Name { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
    public string Served { get; set; } = string.Empty;
    public string Underserved { get; set; } = string.Empty;
    public string Unserved { get; set; } = string.Empty;
}

/// <summary>
/// Overall scoring guide that explains how to interpret total scores.
/// </summary>
public class ScoringGuideDto
{
    public string ServedArea { get; set; } = string.Empty;
    public string UnderservedArea { get; set; } = string.Empty;
    public string UnservedArea { get; set; } = string.Empty;
}
