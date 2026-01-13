namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Defines comparison operators for threshold evaluation in rubric scoring
/// </summary>
public enum ThresholdOperatorEnum
{
    /// <summary>
    /// Greater than or equal to (>=)
    /// </summary>
    GreaterThanOrEqual = 1,

    /// <summary>
    /// Less than (&lt;)
    /// </summary>
    LessThan = 2,

    /// <summary>
    /// Between two values (inclusive)
    /// </summary>
    Between = 3,

    /// <summary>
    /// Equals (=)
    /// </summary>
    Equals = 4,

    /// <summary>
    /// Not equals (!=)
    /// </summary>
    NotEquals = 5,

    /// <summary>
    /// Greater than (&gt;)
    /// </summary>
    GreaterThan = 6,

    /// <summary>
    /// Less than or equal to (&lt;=)
    /// </summary>
    LessThanOrEqual = 7
}
