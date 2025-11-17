namespace NTC.QoS.Infrastructure.Seed.Interfaces;

/// <summary>
/// Transforms parsed GeoJSON features into domain entities.
/// </summary>
/// <typeparam name="TFeature">The parsed feature type.</typeparam>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
public interface ILocationDataTransformer<in TFeature, out TEntity>
{
    /// <summary>
    /// Transforms a parsed feature into a domain entity.
    /// </summary>
    /// <param name="feature">The parsed feature.</param>
    /// <param name="context">Additional context for transformation (e.g., parent IDs).</param>
    /// <returns>The transformed entity, or null if transformation fails.</returns>
    TEntity? Transform(TFeature feature, TransformContext context);
}

/// <summary>
/// Context information for entity transformation.
/// </summary>
public class TransformContext
{
    public Dictionary<string, Guid> ParentIdMap { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string UserId { get; init; } = "System";
}
