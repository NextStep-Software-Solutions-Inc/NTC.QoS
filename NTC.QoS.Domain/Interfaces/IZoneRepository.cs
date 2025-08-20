using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IZoneRepository : IRepository<Zone>
{
    Task<Zone?> FindByNameAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default);
    Task<Zone?> FindFirstAnyMatchAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default);
}