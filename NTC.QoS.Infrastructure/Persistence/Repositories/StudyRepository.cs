using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class StudyRepository(ApplicationDbContext context)
    : Repository<Study>(context), IStudyRepository
{
}
