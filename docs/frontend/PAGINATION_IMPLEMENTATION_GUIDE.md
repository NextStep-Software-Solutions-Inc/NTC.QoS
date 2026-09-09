# URL-Based Pagination Implementation Guide

## Status: 4/10 Complete

### Completed Pages
- ✅ Regions (`/super-admin/regions`)
- ✅ Users (`/super-admin/users`)
- ✅ Roles (`/super-admin/roles`)
- ✅ Permissions (`/super-admin/permissions`)

### Remaining Pages to Update (6)
- [ ] Role Permissions (`/super-admin/role-permissions`)
- [ ] User Regions (`/super-admin/user-regions`)
- [ ] Provinces (`/super-admin/provinces`)
- [ ] Municipalities (`/super-admin/municipalities`)
- [ ] Barangays (`/super-admin/barangays`)
- [ ] Service Types (`/super-admin/service-types`)

## Implementation Pattern

### 1. Add Imports
```typescript
import { useState, useEffect } from "react"; // Add useEffect
import Link from "next/link";
import { ArrowLeft } from "lucide-react"; // Add ArrowLeft to existing imports
import { useUrlPagination } from "@/hooks/useUrlPagination";
import { Pagination } from "@/components/ui/pagination";
```

### 2. Add Hook and Debounced Search
```typescript
const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

// Debounced search
useEffect(() => {
    const timer = setTimeout(() => {
        setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
}, [searchQuery, setSearch]);
```

### 3. Update API Query
**Replace:**
```typescript
queryParams: { pageNumber: 1, pageSize: 1000 }
```

**With:**
```typescript
queryParams: { pageNumber: page, pageSize: pageSize }
```

### 4. Update Data Processing
**Replace:**
```typescript
const items = itemsData?.data ?? [];
const filteredItems = items.filter(item => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return /* search logic */;
});
```

**With:**
```typescript
const items = itemsData?.data ?? [];
const totalCount = itemsData?.totalCount || 0;
const totalPages = Math.ceil(totalCount / pageSize);

const filteredItems = search
    ? items.filter(item => {
        const query = search.toLowerCase();
        return /* search logic */;
    })
    : items;
```

### 5. Add Back to Dashboard Button
**Replace header:**
```typescript
<div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
    <div>
        <h1 className="text-3xl font-bold">Page Title</h1>
        <p className="text-muted-foreground mt-1">Description</p>
    </div>
```

**With:**
```typescript
<div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
    <div className="flex items-center gap-4">
        <Link href="/super-admin">
            <Button variant="ghost" size="sm" className="gap-2">
                <ArrowLeft className="h-4 w-4" />
                Back to Dashboard
            </Button>
        </Link>
        <div>
            <h1 className="text-3xl font-bold">Page Title</h1>
            <p className="text-muted-foreground mt-1">Description</p>
        </div>
    </div>
```

### 6. Update Count Display
**Replace:**
```typescript
Title ({filteredItems.length})
```

**With:**
```typescript
Title ({search ? filteredItems.length : totalCount})
```

### 7. Add Pagination Component
**Add before `</CardContent>`:**
```typescript
{!search && totalCount > 0 && (
    <Pagination
        currentPage={page}
        totalPages={totalPages}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
    />
)}
```

### 8. Ensure Mutations Call refetch()
**Pattern:**
```typescript
toast.promise(
    mutateAsync({ /* params */ }).then(() => refetch()),
    {
        loading: "Processing...",
        success: "Success!",
        error: "Failed"
    }
);
```

## Testing Checklist
For each updated page, verify:
- [ ] Page loads with default pagination (page=1, pageSize=10)
- [ ] URL updates when changing page
- [ ] URL updates when changing page size
- [ ] Search updates URL and filters results
- [ ] Back button navigates to `/super-admin`
- [ ] Browser back/forward buttons work
- [ ] Page refresh maintains state
- [ ] Create/Edit/Delete operations refetch data
- [ ] Pagination controls are functional
- [ ] Mobile responsive layout works

## Infrastructure Files
The following files support URL pagination and are already implemented:
- `src/hooks/useUrlPagination.ts` - URL state management hook
- `src/components/ui/pagination.tsx` - Pagination UI component

## Benefits
✅ URL Persistence - State survives browser refresh
✅ Shareable URLs - Users can share links with filters
✅ Better Performance - Load only one page at a time
✅ Better UX - Clear pagination controls
✅ Navigation - Easy return to dashboard

## Notes
- Default pageSize changed from 1000 → 10
- Search is currently client-side (filters one page of data)
- Future enhancement: Server-side search with API search parameter
- Pagination hides during search (intentional for client-side filtering)
