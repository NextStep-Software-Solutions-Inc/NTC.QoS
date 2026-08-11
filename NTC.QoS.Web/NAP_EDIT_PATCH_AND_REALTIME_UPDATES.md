# NAP Edit PATCH Method & Real-time Updates - Change Log

## Date: February 12, 2026

## Overview
Implemented two critical improvements to the NAP management system:
1. Changed edit NAP operation from PUT to PATCH method to match API specification
2. Enhanced real-time data synchronization by properly invalidating both NAP list and analytics queries after all mutations

---

## Issue 1: Edit NAP Using Wrong HTTP Method

### Problem
The edit NAP handler was using HTTP PUT method, but the backend API expects PATCH method according to the API specification:
- **Endpoint**: `PATCH /NAPs/{id}`
- **Current Implementation**: Used `request.put()`
- **Required Implementation**: Use `request.patch()`

### Solution
Updated [editNAPRequest.ts](src/components/features/NAP/handlers/editNAPRequest.ts) to use PATCH method.

### Changes Made

**File**: `src/components/features/NAP/handlers/editNAPRequest.ts`

```typescript
// BEFORE
export const editNAPRequest: MutationBlueprintFromTemplate<...> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<NAP>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

// AFTER
export const editNAPRequest: MutationBlueprintFromTemplate<...> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<NAP>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
```

### API Specification Match
The payload structure remains the same and correctly matches the API:
```typescript
{
  serviceProviderId: string;  // UUID of wired service provider
  address: string;           // Optional address field
  latitude: number;          // Geographic coordinate
  longitude: number;         // Geographic coordinate
}
```

---

## Issue 2: Incomplete Query Invalidation After Mutations

### Problem
After adding, editing, or deleting NAPs, the analytics panel and NAP list were not always refreshing in real-time. The system had partial invalidation:
- ✅ Some operations invalidated NAP list (`get-NAPs`)
- ❌ Not all operations invalidated analytics (`nap-analytics`)
- ❌ Inconsistent invalidation patterns across different pages and hooks

This led to:
- Stale analytics data after mutations
- Users having to manually refresh to see updated statistics
- Inconsistent UI state across components

### Solution
Implemented comprehensive query invalidation strategy:
1. Updated `useOptimisticNAPs` hook to invalidate both queries after all operations
2. Updated page-level handlers to invalidate both queries after edit operations
3. Ensured consistent behavior across NAP Management Page and NAP Map Page

---

## Changes Made

### 1. useOptimisticNAPs Hook

**File**: `src/hooks/useOptimisticNAPs.tsx`

Enhanced all three mutation operations to invalidate both NAP list and analytics:

#### Add NAP Operation
```typescript
// BEFORE
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });

toast.success("NAP created successfully!");

// AFTER
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics" });

toast.success("NAP created successfully!");
```

#### Update NAP Operation
```typescript
// BEFORE
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });

toast.success("NAP updated successfully!");

// AFTER
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics" });

toast.success("NAP updated successfully!");
```

#### Delete NAP Operation
```typescript
// BEFORE
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });

toast.success("NAP deleted successfully!");

// AFTER
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });
void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics" });

toast.success("NAP deleted successfully!");
```

### 2. NAP Management Page

**File**: `src/components/features/NAP/pages/NAPsManagementPage.tsx`

Updated edit dialog success handler:

```typescript
// BEFORE
<EditNAPDialog
  nap={selectedNAP}
  open={editDialogOpen}
  onOpenChange={setEditDialogOpen}
  onSuccess={() => {
    setSelectedNAP(null);
    setEditDialogOpen(false);
    void refetchNAPs();  // Only refetched NAPs
  }}
/>

// AFTER
<EditNAPDialog
  nap={selectedNAP}
  open={editDialogOpen}
  onOpenChange={setEditDialogOpen}
  onSuccess={() => {
    setSelectedNAP(null);
    setEditDialogOpen(false);
    // Invalidate both NAP list and analytics for real-time updates
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics" });
  }}
/>
```

### 3. NAP Map Page

**File**: `src/components/features/NAP/pages/NAPMapPage.tsx`

Updated edit dialog success handler:

```typescript
// BEFORE
<EditNAPDialog
  nap={selectedNAP}
  open={editDialogOpen}
  onOpenChange={setEditDialogOpen}
  onSuccess={() => {
    setSelectedNAP(null);
    setEditDialogOpen(false);
    void refetchNAPs();  // Only refetched NAPs
  }}
/>

// AFTER
<EditNAPDialog
  nap={selectedNAP}
  open={editDialogOpen}
  onOpenChange={setEditDialogOpen}
  onSuccess={() => {
    setSelectedNAP(null);
    setEditDialogOpen(false);
    // Invalidate both NAP list and analytics for real-time updates
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs" });
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics" });
  }}
/>
```

---

## Query Invalidation Strategy

### What Gets Invalidated

#### `get-NAPs` Query
- **Purpose**: NAP list data for table, map markers, and filters
- **When Invalidated**: After create, update, delete operations
- **Effect**: Triggers refetch of NAP list with current filters

#### `nap-analytics` Query
- **Purpose**: Analytics dashboard data (market share, provider comparison, statistics)
- **When Invalidated**: After create, update, delete operations
- **Effect**: Triggers refetch of analytics data with current filters

### Query Invalidation Flow

```
User Action (Add/Edit/Delete NAP)
        ↓
Optimistic UI Update (Immediate)
        ↓
API Call (Background)
        ↓
Success Handler
        ↓
Invalidate Queries:
  ├─ get-NAPs → Refetch NAP list
  └─ nap-analytics → Refetch analytics
        ↓
UI Updates (Real-time)
```

---

## Impact on User Experience

### Before Changes
1. ❌ Edit NAP failed with wrong HTTP method
2. ❌ Analytics showed stale data after mutations
3. ❌ Users had to manually refresh page to see updates
4. ❌ Inconsistent data between NAP list and analytics

### After Changes
1. ✅ Edit NAP works correctly with PATCH method
2. ✅ Analytics automatically update after all mutations
3. ✅ Real-time data synchronization across all components
4. ✅ Consistent data state throughout the application
5. ✅ No manual refresh needed
6. ✅ Optimistic updates provide instant feedback

---

## Files Modified

1. **src/components/features/NAP/handlers/editNAPRequest.ts**
   - Changed HTTP method from PUT to PATCH

2. **src/hooks/useOptimisticNAPs.tsx**
   - Added analytics query invalidation after create operation
   - Added analytics query invalidation after update operation
   - Added analytics query invalidation after delete operation

3. **src/components/features/NAP/pages/NAPsManagementPage.tsx**
   - Updated edit success handler to invalidate both queries

4. **src/components/features/NAP/pages/NAPMapPage.tsx**
   - Updated edit success handler to invalidate both queries

---

## Testing Checklist

### Edit NAP with PATCH Method
- [x] Edit a NAP's location
- [x] Verify PATCH request is sent (check network tab)
- [x] Verify edit succeeds without errors
- [x] Verify payload matches API spec

### Real-time Updates After Add NAP
- [x] Add a new NAP
- [x] Verify NAP appears in list immediately (optimistic)
- [x] Verify NAP persists after API success
- [x] Verify analytics panel updates automatically
- [x] Verify total count updates
- [x] Verify provider statistics update

### Real-time Updates After Edit NAP
- [x] Edit an existing NAP
- [x] Verify changes appear immediately in list
- [x] Verify changes appear on map
- [x] Verify analytics update if provider changed
- [x] Verify no manual refresh needed

### Real-time Updates After Delete NAP
- [x] Delete a NAP
- [x] Verify NAP removed from list immediately
- [x] Verify NAP removed from map
- [x] Verify analytics panel updates
- [x] Verify total count decrements
- [x] Verify provider statistics update

### Analytics Synchronization
- [x] Perform any mutation (add/edit/delete)
- [x] Verify market share chart updates
- [x] Verify provider comparison updates
- [x] Verify summary statistics update
- [x] Verify geographic coverage updates (if enabled)

---

## Technical Notes

### React Query Cache Invalidation
Using predicate-based invalidation ensures we catch all queries with a specific key prefix:
```typescript
void queryClient.invalidateQueries({ 
  predicate: (query) => query.queryKey[0] === "get-NAPs" 
});
```

This is more flexible than exact key matching because:
- Works with different query parameters
- Handles paginated queries
- Catches filtered queries
- Future-proof for additional query variations

### Optimistic Updates
The `useOptimisticNAPs` hook provides instant UI feedback:
1. **Add**: Shows temporary NAP immediately, replaces with server data on success
2. **Update**: Shows updated NAP immediately, confirms with server data
3. **Delete**: Removes NAP immediately, rolls back on error

This creates a snappy, responsive user experience even with slow network connections.

### Error Handling
If mutations fail:
- Optimistic updates are rolled back
- User sees error toast notification
- Previous data state is restored
- No query invalidation occurs (prevents showing stale data)

---

## API Specification Reference

### Edit NAP Endpoint
```
PATCH /NAPs/{id}

Parameters:
- id (path): string($uuid) - NAP identifier

Request Body (application/json):
{
  "serviceProviderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "address": "string",
  "latitude": 0,
  "longitude": 0
}

Response:
- 200: Success with updated NAP data
- 400: Validation error
- 404: NAP not found
```

---

## Performance Considerations

### Query Invalidation Overhead
- **Minimal**: React Query intelligently batches refetch requests
- **Optimized**: Only affected queries are refetched
- **Cached**: Repeated operations use cached data when appropriate

### Network Impact
- **Reduced**: Optimistic updates minimize perceived network delay
- **Efficient**: Single invalidation triggers batch refetch
- **Smart**: React Query deduplicates concurrent requests

---

## Future Enhancements

1. **Selective Invalidation**: Only invalidate analytics if provider/location changed significantly
2. **Incremental Updates**: Update cache directly instead of full refetch for small changes
3. **Real-time Subscriptions**: Consider WebSocket for multi-user environments
4. **Offline Support**: Queue mutations when offline, sync when connection restored

---

## Related Documentation
- [NAP Management Implementation](NAP_MANAGEMENT.md)
- [NAP Forms and Analytics Fixes](NAP_FORMS_AND_ANALYTICS_FIX.md)
- [Optimistic Updates Pattern](OPTIMISTIC_UPDATES.md)
- [React Query Invalidation Guide](https://tanstack.com/query/latest/docs/react/guides/query-invalidation)

---

## Summary

All requested changes have been successfully implemented:

### ✅ Issue 1: PATCH Method
- Changed edit NAP handler from PUT to PATCH
- Matches API specification exactly
- Tested and working without errors

### ✅ Issue 2: Real-time Updates
- All mutations now invalidate both NAP list and analytics queries
- Consistent invalidation across all pages and hooks
- Immediate UI updates after all operations
- No manual refresh needed

The NAP management system now provides:
- ✅ Correct HTTP method usage (PATCH for updates)
- ✅ Real-time data synchronization
- ✅ Consistent UI state across all components
- ✅ Optimistic updates for instant feedback
- ✅ Automatic analytics refresh
- ✅ Production-ready error handling

Users will experience seamless, real-time updates throughout the NAP management interface with no need for manual page refreshes.
