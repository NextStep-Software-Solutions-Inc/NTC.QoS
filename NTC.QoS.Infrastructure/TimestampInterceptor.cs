using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure;
public class TimestampInterceptor : ISaveChangesInterceptor
{
    private readonly IUserContextService _userContextService;

    public TimestampInterceptor(IUserContextService userContextService) // Constructor injection
    {
        _userContextService = userContextService;
    }
    public void SaveChangesFailed(DbContextErrorEventData eventData) { }

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context == null) return result;

        var userId = _userContextService.GetCurrentUserId();
        var entries = eventData.Context.ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity entity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedAt = DateTime.UtcNow;
                        entity.CreatedBy = userId ?? "System";
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.CreatedBy = userId ?? "System"; ;
                        entity.IsDeleted = false; // Initialize IsDeleted to false on creation
                        break;
                    case EntityState.Modified:
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.UpdatedBy = userId ?? "System";
                        break;
                    case EntityState.Deleted: // Handle soft delete
                        entry.State = EntityState.Modified; // Change state to Modified
                        entity.IsDeleted = true; // Set IsDeleted to true
                        entity.DeletedAt = DateTime.UtcNow;
                        entity.DeletedBy = userId ?? "System";
                        break;
                }
            }
        }

        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null) return new ValueTask<InterceptionResult<int>>(result);

        var userId = _userContextService.GetCurrentUserId();
        var entries = eventData.Context.ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity entity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedAt = DateTime.UtcNow;
                        entity.CreatedBy = userId ?? "System";
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.CreatedBy = userId ?? "System";
                        entity.IsDeleted = false; // Initialize IsDeleted to false on creation
                        break;
                    case EntityState.Modified:
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.UpdatedBy = userId ?? "System";
                        break;
                    case EntityState.Deleted: // Handle soft delete
                        entry.State = EntityState.Modified; // Change state to Modified
                        entity.IsDeleted = true; // Set IsDeleted to true
                        entity.DeletedAt = DateTime.UtcNow;
                        entity.DeletedBy = userId ?? "System";
                        break;
                }
            }
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }
}
