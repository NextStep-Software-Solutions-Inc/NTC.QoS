# Super Admin Console - Remaining Implementation Guide

## Status: 80% Complete

### ✅ COMPLETED
1. **All Handler Updates** (8 files)
   - Regions: create + edit handlers with FormData
   - Provinces: create + edit handlers with FormData
   - Municipalities: create + edit handlers with FormData
   - Barangays: create + edit handlers with FormData
   - All handlers have null payload checks
   - All handlers support boundariesFile upload

2. **Type Definitions Updated** (4 files)
   - All geographic entity types include `boundariesFile?: File`

3. **Municipalities Page** - FULLY IMPLEMENTED
   - URL-based pagination ✅
   - Debounced search (300ms) ✅
   - Pagination controls ✅
   - GeoJSON file upload UI ✅
   - Complete CRUD with file support ✅

### 🚧 REMAINING WORK

#### 1. Barangays Page (HIGH PRIORITY)
**Location:** `src/app/(super_admin)/super-admin/barangays/page.tsx`

**Changes Needed:**
```typescript
// Add imports
import { useEffect } from "react";
import { useUrlPagination } from "@/hooks/useUrlPagination";
import { Pagination } from "@/components/ui/pagination";

// Add pagination state
const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();
const [boundariesFile, setBoundariesFile] = useState<File | null>(null);

// Add debounced search
useEffect(() => {
    const timer = setTimeout(() => setSearch(searchQuery), 300);
    return () => clearTimeout(timer);
}, [searchQuery, setSearch]);

// Update query
queryParams: { pageNumber: page, pageSize: pageSize }

// Remove filtered barangays, use direct data
const barangays = barangaysData?.data ?? [];
const totalCount = barangaysData?.totalRows || 0;

// Add file input in create/edit dialogs
<Label htmlFor="boundariesFile">Boundaries File (GeoJSON)</Label>
<Input
    id="boundariesFile"
    type="file"
    accept=".json,.geojson"
    onChange={(e) => setBoundariesFile(e.target.files?.[0] || null)}
/>

// Pass in payload
boundariesFile: boundariesFile || undefined

// Add pagination component
{!isLoading && !search && totalCount > 0 && (
    <Pagination
        currentPage={page}
        totalPages={Math.ceil(totalCount / pageSize)}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
    />
)}
```

#### 2. User Regions Page
**Location:** `src/app/(super_admin)/super-admin/user-regions/page.tsx`

**Changes Needed:**
```typescript
// Add pagination (same pattern as above, but NO file upload)
const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

// Update queries to use page/pageSize
queryParams: { pageNumber: page, pageSize: pageSize }

// Remove filtered logic
const userRegions = userRegionsData?.data ?? [];
const totalCount = userRegionsData?.totalRows || 0;

// Add pagination component
```

#### 3. Role Permissions Page
**Location:** `src/app/(super_admin)/super-admin/role-permissions/page.tsx`

**Changes Needed:**
```typescript
// Same as User Regions - add pagination, NO file upload needed
```

#### 4. Regions Page
**Location:** `src/app/(super_admin)/super-admin/regions/page.tsx`

**Changes Needed:**
```typescript
// Already has URL pagination ✅
// Just needs file upload UI added to forms
const [boundariesFile, setBoundariesFile] = useState<File | null>(null);

// Add file input in dialogs (same as municipalities)
// Pass boundariesFile in create/edit calls
```

#### 5. Provinces Page
**Location:** `src/app/(super_admin)/super-admin/provinces/page.tsx`

**Changes Needed:**
```typescript
// Already has URL pagination ✅
// Just needs file upload UI added to forms
const [boundariesFile, setBoundariesFile] = useState<File | null>(null);

// Add file input in dialogs (same as municipalities)
// Pass boundariesFile in create/edit calls
```

## Implementation Priority

1. **Barangays** (needs both pagination + file upload)
2. **User Regions** (needs pagination only)
3. **Role Permissions** (needs pagination only)
4. **Regions** (needs file upload UI only)
5. **Provinces** (needs file upload UI only)

## Testing Checklist

After completing each page:
- [ ] URL parameters update when changing page/pageSize
- [ ] Search query appears in URL
- [ ] Browser back/forward works correctly
- [ ] Page state survives refresh
- [ ] File input accepts .json and .geojson files
- [ ] File can be cleared before submission
- [ ] Create/Edit operations succeed with file
- [ ] Create/Edit operations succeed without file
- [ ] Pagination shows correct page numbers
- [ ] Page size changes reset to page 1
- [ ] Total count displays correctly

## Build Command
```bash
cd /home/runner/work/ntc_qos-fe/ntc_qos-fe
pnpm install
NEXT_TURBOPACK_EXPERIMENTAL_USE_SYSTEM_TLS_CERTS=1 pnpm build
```

## Notes
- All backend handlers are COMPLETE and support file uploads
- Municipalities page serves as the REFERENCE IMPLEMENTATION
- Pattern is consistent across all pages
- File uploads are OPTIONAL in all cases
- Pagination is URL-based for shareability
