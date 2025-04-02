using NTC.QoS.Domain.Aggregates;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IStudyMunicipalityRepository : IRepository<StudyMunicipality>
{
    new Task<List<StudyMunicipality>> ListAsync(Expression<Func<StudyMunicipality, bool>>? expression = null, CancellationToken cancellationToken = default);
}
