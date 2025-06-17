using System.Linq.Expressions;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Application.Services;

public interface IGeoSpatialService
{
    bool IsPointWithinProvince(Point location, Province province);
    bool IsPointWithinRegion(Point location, Region region);
}
