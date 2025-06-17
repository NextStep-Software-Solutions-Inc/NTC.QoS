using System.Linq.Expressions;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Infrastructure.Services;

public class GeospatialService : IGeoSpatialService
{
    public bool IsPointWithinProvince(Point location, Province province)
    {
        return province.Boundaries != null && province.Boundaries.Contains(location);
    }

    public bool IsPointWithinRegion(Point location, Region region)
    {
        return region.Boundaries != null && region.Boundaries.Contains(location);
    }
}
