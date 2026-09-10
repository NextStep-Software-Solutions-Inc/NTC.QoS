using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(ApplicationDbContext context)
    : Repository<Notification>(context), INotificationRepository
{
}
