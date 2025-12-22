namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for loading global AI instruction/context
/// </summary>
public interface IAIInstructionService
{
    /// <summary>
    /// Gets the global AI system instruction/context that should be included in all AI requests
    /// </summary>
    /// <returns>The global AI instruction content</returns>
    string GetGlobalInstruction();
}
