using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for objects that have a Location property.
/// Provides explicit type contract for better type safety than generic constraints with reflection.
/// This is used in analytics handlers to ensure type safety when accessing location data.
/// </summary>
public interface IHasLocation
{
    Point Location { get; }
}