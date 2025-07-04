using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Interfaces;

public interface IGeoLocatedEntity
{
    Point Location { get; }
}
