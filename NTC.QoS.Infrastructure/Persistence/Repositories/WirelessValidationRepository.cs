using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class WirelessValidationRepository(ApplicationDbContext context) : Repository<WirelessValidation>(context), IWirelessValidationRepository
{
}
