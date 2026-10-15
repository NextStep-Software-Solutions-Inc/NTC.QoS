# Technical Debt & Future Improvements

## Known Issues to Address

### 1. Pagination & Performance (Priority: HIGH)

**Issue**: Several pages fetch large datasets (pageSize: 1000) which could cause performance issues.

**Affected Files**:
- `src/app/(super_admin)/super-admin/users/page.tsx`
- `src/app/(super_admin)/super-admin/user-regions/page.tsx`
- `src/app/(super_admin)/super-admin/user-permissions/page.tsx`
- All other list pages

**Impact**:
- Memory usage on client
- Slow initial page load
- Server load with large datasets
- Poor user experience with many records

**Recommended Solutions**:
1. **Server-Side Pagination**:
   - Implement proper pagination controls (prev/next, page numbers)
   - Use reasonable default page size (e.g., 20-50)
   - Add page size selector (10, 25, 50, 100)
   
2. **Infinite Scrolling**:
   - Use React Query's `useInfiniteQuery`
   - Load more data as user scrolls
   - Better UX for browsing large lists

3. **Virtual Scrolling**:
   - For very large lists, use virtual scrolling libraries
   - Only render visible rows
   - Significant performance improvement

4. **Server-Side Search & Filtering**:
   - Move search from client to server
   - Reduce data transfer
   - Better performance with large datasets

**Example Implementation** (Server-Side Pagination):
```tsx
const [page, setPage] = useState(1);
const [pageSize, setPageSize] = useState(25);

const { data, isLoading } = useGetAllUsers({
    pathParams: {},
    queryParams: { 
        pageNumber: page, 
        pageSize: pageSize,
        search: searchQuery // Server-side search
    }
});

// Add pagination controls
<div className="flex items-center justify-between mt-4">
    <Button 
        onClick={() => setPage(p => Math.max(1, p - 1))}
        disabled={page === 1}
    >
        Previous
    </Button>
    <span>Page {page} of {totalPages}</span>
    <Button 
        onClick={() => setPage(p => p + 1)}
        disabled={page === totalPages}
    >
        Next
    </Button>
</div>
```

### 2. Multiple Concurrent Requests

**Issue**: Some pages make multiple large API requests simultaneously (users, permissions, regions).

**Affected Files**:
- `src/app/(super_admin)/super-admin/user-permissions/page.tsx` (fetches users + permissions)
- `src/app/(super_admin)/super-admin/user-regions/page.tsx` (fetches users + regions)

**Solutions**:
1. **Lazy Loading Dropdowns**:
   - Load dropdown data only when dialog opens
   - Use search/autocomplete for large lists
   - Example: `react-select` with async loading

2. **Request Staggering**:
   - Load primary data first
   - Load secondary data after initial render
   - Use `enabled` option in React Query

3. **Caching Strategy**:
   - Leverage React Query cache across components
   - Set appropriate `staleTime` and `cacheTime`
   - Reduce redundant API calls

**Example** (Lazy Loading):
```tsx
// Only fetch when dialog is open
const { data: usersData } = useGetUsers({
    pathParams: {},
    queryParams: { pageSize: 100 },
    enabled: createDialogOpen // Only fetch when needed
});
```

### 3. Client-Side Search/Filter

**Issue**: All search is done client-side after fetching all data.

**Current Implementation**:
```tsx
const filteredItems = items.filter(item => 
    item.name.toLowerCase().includes(searchQuery.toLowerCase())
);
```

**Recommended**:
- Move search to backend
- Update query params when search changes
- Debounce search input (300-500ms)

**Example**:
```tsx
const [debouncedSearch] = useDebounce(searchQuery, 300);

const { data } = useGetItems({
    queryParams: {
        search: debouncedSearch, // Backend handles filtering
        pageSize: 25
    }
});
```

### 4. Form Validation

**Status**: Basic HTML validation only

**Improvements Needed**:
- Schema-based validation (Zod)
- Async validation (check unique values)
- Better error messages
- Field-level validation feedback

**Example** (with react-hook-form + Zod):
```tsx
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

const schema = z.object({
    name: z.string().min(3, "Name must be at least 3 characters"),
    key: z.string().regex(/^[a-z_]+$/, "Key must be lowercase with underscores"),
    description: z.string().optional()
});

const form = useForm({
    resolver: zodResolver(schema)
});
```

### 5. Error Handling

**Current**: Generic error messages in toast notifications

**Improvements**:
- Parse API error responses
- Show specific error messages
- Field-level errors for forms
- Retry logic for failed requests
- Error boundaries for component errors

### 6. Loading States

**Current**: Simple "Loading..." text

**Improvements**:
- Skeleton loaders for better UX
- Optimistic updates for mutations
- Inline loading indicators
- Progress bars for long operations

### 7. Accessibility

**To Review**:
- Keyboard navigation
- Screen reader support
- ARIA labels
- Focus management
- Color contrast

### 8. Testing

**Missing**:
- Unit tests for components
- Integration tests for CRUD flows
- E2E tests for user workflows
- Mobile responsiveness tests

**Recommended Testing Strategy**:
```typescript
// Component test example
describe('RolesManagementPage', () => {
    it('should display roles list', async () => {
        // Mock API response
        // Render component
        // Assert roles are displayed
    });

    it('should create new role', async () => {
        // Mock API
        // Click create button
        // Fill form
        // Submit
        // Assert success
    });
});
```

### 9. Code Duplication

**Issue**: Similar CRUD logic repeated across pages

**Solution**:
- Create generic CRUD table component
- Extract common form patterns
- Shared dialog components
- Reusable hooks for common operations

**Example** (Generic CRUD Hook):
```tsx
function useCrud<T>(config: CrudConfig<T>) {
    // Common CRUD logic
    // Shared state management
    // Reusable handlers
    return {
        items,
        isLoading,
        create,
        update,
        delete,
        refresh
    };
}
```

### 10. Type Safety

**Improvements**:
- Stricter types for API responses
- Better error type definitions
- Type guards for runtime checks
- Discriminated unions for states

## Implementation Priority

### Phase 1 (Critical - Before Production)
1. ✅ Implement proper pagination
2. ✅ Add server-side search
3. ✅ Fix concurrent request issues
4. ✅ Add proper error handling

### Phase 2 (Important - Soon After Launch)
5. Add form validation with Zod
6. Implement better loading states
7. Add basic unit tests
8. Accessibility audit and fixes

### Phase 3 (Enhancements)
9. Reduce code duplication
10. Add advanced features (bulk operations, exports, etc.)

## Notes

- These issues are documented for future sprints
- Current implementation is functional and follows best practices
- Improvements should be prioritized based on actual usage patterns
- Some items may not be needed if data volumes remain small

## When to Address

**Before API Integration**:
- Review actual API pagination capabilities
- Understand data volume expectations
- Confirm backend search/filter support

**During API Integration**:
- Implement pagination if backend supports it
- Add proper error handling for API errors
- Adjust page sizes based on actual data

**After Initial Release**:
- Monitor performance metrics
- Gather user feedback
- Prioritize improvements based on real usage
