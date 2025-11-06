namespace NTC.QoS.Domain.Interfaces;

public interface IFileRepository : IRepository<Domain.Entities.File>
{
    /// <summary>
    /// Applies case-insensitive search filter on file name
    /// </summary>
    IQueryable<Domain.Entities.File> SearchByName(IQueryable<Domain.Entities.File> query, string name);
}
