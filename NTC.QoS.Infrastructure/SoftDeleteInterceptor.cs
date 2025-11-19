using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure;

public class SoftDeleteInterceptor : ISaveChangesInterceptor
{
    private readonly IUserContextService _userContextService;

    public SoftDeleteInterceptor(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    public void SaveChangesFailed(DbContextErrorEventData eventData) { }

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context == null) return result;
        ApplySoftDelete(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
            ApplySoftDelete(eventData.Context);
        return new(result);
    }

    private void ApplySoftDelete(DbContext context)
    {
        var userId = _userContextService.GetCurrentUserId() ?? "System";
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is not IHardDelete)
            {
                if (!entry.Entity.IsDeleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                }
                else
                {
                    // Already soft-deleted; prevent unintended repeated mutation
                    entry.State = EntityState.Unchanged;
                }
            }
        }
    }
}
