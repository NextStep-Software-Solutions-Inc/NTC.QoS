# NAP Delete Endpoint Fix - Summary

## Problem Statement
The NAP delete handler was sending DELETE requests to `/NAPs/:id` (using path parameters), but the backend expects DELETE requests to `/NAPs?ids=...` (using query parameters) for both single and bulk deletion. This mismatch caused a **405 Method Not Allowed error**, preventing users from deleting NAPs in the portal.

## Solution Overview
Updated the frontend to send DELETE requests to `/NAPs?ids=...` for both single and bulk delete operations:
- **Single delete**: `/NAPs?ids=<id>`
- **Bulk delete**: `/NAPs?ids=<id1>&ids=<id2>&ids=<id3>` (multiple query params with same key)

## Files Changed

### 1. `src/components/features/NAP/handlers/deleteNAPRequest.ts`
**Before:**
```typescript
const endpoint = "/NAPs/:id";

export const deleteNAPRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<NAP>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<NAP>>(finalUrl);
  },
};
```

**After:**
```typescript
const endpoint = "/NAPs";

type DeleteNAPQueryParams = {
  ids: string | string[];  // Supports both single ID and array of IDs
};

export const deleteNAPRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<NAP>,
  DeleteNAPQueryParams
> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<NAP>>(finalUrl);
  },
};
```

### 2. `src/components/features/NAP/dialogs/DeleteNAPDialog.tsx`
**Before:**
```typescript
toast.promise(mutateAsync({ pathParams: { id: nap.id } }), {
  // ...
});
```

**After:**
```typescript
toast.promise(mutateAsync({ pathParams: {}, queryParams: { ids: nap.id } }), {
  // ...
});
```

### 3. `src/components/features/NAP/pages/NAPMapPage.tsx`
**Before:**
```typescript
await deleteNAPMutation.mutateAsync({ pathParams: { id: napToDelete.id } });
```

**After:**
```typescript
await deleteNAPMutation.mutateAsync({ pathParams: {}, queryParams: { ids: napToDelete.id } });
```

### 4. `src/components/features/NAP/pages/NAPsManagementPage.tsx`
**Before (inefficient - multiple API calls):**
```typescript
const results = await Promise.allSettled(
  idsToDelete.map((id) => deleteNAPMutation.mutateAsync({ pathParams: { id } })),
);

const successCount = results.filter((r) => r.status === "fulfilled").length;
const failureCount = results.filter((r) => r.status === "rejected").length;

if (successCount > 0) {
  toast.success(`Deleted ${successCount} NAP(s)`);
}
if (failureCount > 0) {
  toast.error(`Failed to delete ${failureCount} NAP(s)`);
}
```

**After (efficient - single API call with array of ids):**
```typescript
// Pass ids as an array for bulk delete
await deleteNAPMutation.mutateAsync({ pathParams: {}, queryParams: { ids: idsToDelete } });

toast.success(`Deleted ${idsToDelete.length} NAP(s)`);
```

**Generated URL:** `/NAPs?ids=<id1>&ids=<id2>&ids=<id3>`

### 5. `src/components/features/NAP/forms/EditNAPForm.tsx`
**Before:**
```typescript
await deleteNAPMutation.mutateAsync({ pathParams: { id: nap.id } });
```

**After:**
```typescript
await deleteNAPMutation.mutateAsync({ pathParams: {}, queryParams: { ids: nap.id } });
```

## Key Improvements

### 1. **Endpoint Alignment**
- Fixed the mismatch between frontend and backend expectations
- Requests now use the correct `/NAPs?ids=...` format

### 2. **Performance Enhancement**
- Bulk delete operations now use a **single API call** instead of multiple sequential calls
- Reduces network overhead and improves deletion speed

### 3. **Type Safety**
- Added `DeleteNAPQueryParams` type to ensure type-safe query parameters
- TypeScript compiler now validates the correct usage

### 4. **Consistency**
- All deletion operations (dialog, map, table, form) now use the same endpoint pattern
- Easier to maintain and debug

## Testing & Validation

### ✅ Code Review
- No issues found
- All changes follow the project's conventions and patterns

### ✅ Security Check (CodeQL)
- Analysis completed with **0 alerts**
- No security vulnerabilities detected

### ✅ Linter
- All files pass ESLint checks
- Code formatting follows project standards

## Technical Details

### How Query Parameters Work
The `buildEndpoint` function in `src/lib/endpoint-builder.ts` automatically converts query parameters to URL query strings. When an array is passed, it appends multiple query parameters with the same key:

```typescript
// For single delete (string)
{ queryParams: { ids: "abc123" } }
// → /NAPs?ids=abc123

// For bulk delete (array)
{ queryParams: { ids: ["abc123", "def456", "ghi789"] } }
// → /NAPs?ids=abc123&ids=def456&ids=ghi789
```

### Endpoint Pattern
```
Single: DELETE /NAPs?ids=<id>
Bulk:   DELETE /NAPs?ids=<id1>&ids=<id2>&ids=<id3>
```

Query parameters are serialized using `URLSearchParams`, which handles:
- Encoding special characters
- Multiple query parameters with the same key (for array values)
- Standard HTTP query string format

## Expected Outcome
✅ Users can now successfully delete NAPs from the portal without encountering **405 Method Not Allowed errors**

✅ Bulk delete operations are more efficient with a single API call

✅ All delete operations across the application (table, map, dialogs, forms) work consistently

## Related References
- Issue: [Frontend] Quick Fix: Change NAP Delete Endpoint to Use Query Param ?ids=...
- Handler: `src/components/features/NAP/handlers/deleteNAPRequest.ts`
- Backend expects: `DELETE /NAPs?ids=...` format for both single and bulk deletion
