using NetTopologySuite.Geometries;
namespace NTC.QoS.Domain.Interfaces;

public interface IGeoBoundaryEntity
{
    Guid Id { get; }
    MultiPolygon? Boundaries { get; }
}
