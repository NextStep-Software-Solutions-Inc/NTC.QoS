using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class StudyMunicipalityRepository(ApplicationDbContext context)
    : Repository<StudyMunicipality>(context), IStudyMunicipalityRepository
{
}

