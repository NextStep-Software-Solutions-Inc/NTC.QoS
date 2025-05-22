using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Domain.Interfaces;

public interface INAPRepository : IRepository<NAP>
{
    Task<List<ClusteredNapResultDto>> GetClusteredNapsAsync(double clusteringDistance, int minPoints = 2);
}
