using LinqKit;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Helper.Spatial;

public static class SpatialFilterBuilder
{
    public static Expression<Func<T, bool>> BuildWithinBoundariesPredicate<T>(IEnumerable<MultiPolygon> multipolygons)
        where T : IGeoLocatedEntity
    {
        var predicate = PredicateBuilder.New<T>(false);

        foreach (var multiPolygon in multipolygons)
        {
            // Loop through each individual polygon in the MultiPolygon
            for (int i = 0; i < multiPolygon.NumGeometries; i++)
            {
                var singlePolygon = multiPolygon.GetGeometryN(i);
                predicate = predicate.Or(entity => entity.Location.IsWithinDistance(singlePolygon, 0));
            }
        }

        return predicate;
    }

    public static Expression<Func<T, bool>> BuildWithinDistancePredicate<T>(Point center, double distance)
        where T : IGeoLocatedEntity
    {
        return entity => entity.Location.IsWithinDistance(center, distance);
    }
}
