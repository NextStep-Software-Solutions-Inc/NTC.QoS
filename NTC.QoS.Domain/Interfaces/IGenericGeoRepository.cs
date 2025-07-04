using NTC.QoS.Domain.Interfaces;

public interface IGenericGeoRepository<T> where T : IGeoBoundaryEntity
{
    IQueryable<T> Query();
}
