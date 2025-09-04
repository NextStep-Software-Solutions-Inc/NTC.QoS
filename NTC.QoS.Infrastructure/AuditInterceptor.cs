using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure;

public class AuditInterceptor : ISaveChangesInterceptor
{
    private readonly IUserContextService _userContextService;
    private readonly ITimeProvider _timeProvider;

    public AuditInterceptor(IUserContextService userContextService, ITimeProvider timeProvider)
    {
        _userContextService = userContextService;
        _timeProvider = timeProvider;
    }

    public void SaveChangesFailed(DbContextErrorEventData eventData) { }

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context == null) return result;
        ApplyAudit(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
            ApplyAudit(eventData.Context);
        return new(result);
    }

    private void ApplyAudit(DbContext context)
    {
        var userId = _userContextService.GetCurrentUserId() ?? "System";
        var now = _timeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Modified:
                    // Avoid overwriting Created* fields
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
