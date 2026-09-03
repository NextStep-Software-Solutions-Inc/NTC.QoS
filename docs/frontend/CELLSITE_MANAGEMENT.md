# Cell Site Management Feature

## Overview

The Cell Site Management feature provides a comprehensive, modern administrative interface for managing cell tower infrastructure. This implementation replaces the previous cellsite page with an optimized, feature-rich solution designed for administrative efficiency.

## Key Features

### 🎯 Core Functionality

1. **Advanced Pagination**
   - Next/Previous navigation
   - First/Last page jumps
   - Custom page jump (go to specific page)
   - Configurable page size (10, 20, 50, 100 items per page)
   - Real-time total count display

2. **Multi-Level Filtering**
   - **Status**: Active, Inactive, Maintenance
   - **Provider**: Filter by service provider
   - **Province**: Filter by province
   - **Municipality**: Cascading filter (requires province selection)
   - **Barangay**: Cascading filter (requires municipality selection)
   - Clear all filters with one click

3. **Smart Search**
   - Search by coordinates (latitude/longitude)
   - Search by provider name
   - Search by site ID
   - Real-time search with debouncing

4. **URL Persistence**
   - All filters persist in URL
   - Pagination state saved in URL
   - Search terms saved in URL
   - Shareable links maintain exact state
   - Page refresh preserves all settings

5. **Batch Operations**
   - Multi-select with checkboxes
   - Batch delete with confirmation
   - Batch status update (Active/Inactive/Maintenance)
   - Visual feedback for selected items

6. **Modern UI/UX**
   - Colorful gradient hero section
   - Provider logos with brand colors
   - Vibrant status badges
   - Smooth animations and transitions
   - Responsive design
   - Dark mode support

### 🎨 Visual Design

#### Color Scheme
- **Active Status**: Emerald green (#10B981)
- **Inactive Status**: Slate gray (#64748B)
- **Maintenance Status**: Amber yellow (#F59E0B)
- **Verified Badge**: Blue (#3B82F6)
- **Hero Gradient**: Blue → Indigo → Purple

#### Provider Display
- Displays provider logo (if available)
- Shows provider initials with brand color (if no logo)
- Brand color border for visual consistency

#### Status Indicators
- Color-coded badges with icons
- Visual hierarchy for quick scanning
- Consistent styling across all views

## Routing Structure

### New Independent Route
```
/admin/cellsites
```

This route is **independent** of the previous dynamic routing pattern (`[region]/[table]/[province]`), providing:
- Better maintainability
- Clearer URL structure
- Easier to extend with new features
- No region dependency for global admin view

### Detail View Route (Placeholder)
```
/admin/cellsites/[id]
```

Prepared for future implementation of detailed cell site views.

## Technical Architecture

### Component Structure

```
src/app/(authenticated)/admin/cellsites/
├── layout.tsx                    # Layout with metadata
├── page.tsx                      # Main page with state management
└── [id]/
    └── page.tsx                  # Detail view (placeholder)

src/components/features/cell/tables/CellsitesTable/
├── index.tsx                     # Barrel export
├── table.tsx                     # Main table component
├── columns.tsx                   # Column definitions
├── toolbar.tsx                   # Filters and batch actions
└── actions.tsx                   # Row-level actions
```

### State Management

**URL State Synchronization**:
- Uses Next.js `useSearchParams` hook
- Automatic URL updates on state changes
- Initializes state from URL on page load
- Supports browser back/forward navigation

**Supported URL Parameters**:
```typescript
?page=2
&pageSize=50
&search=Globe
&status=active
&provider=provider-id
&province=province-id
&municipality=municipality-id
&barangay=barangay-id
```

### Data Fetching

**React Query Integration**:
- Server-side pagination
- Automatic refetch on filter changes
- Loading states
- Error handling
- Cache management

**API Handlers Used**:
- `getCellsRequest` - Fetch cell sites with filters
- `deleteCellsRequest` - Batch delete cells
- `editCellStatusRequest` - Update cell status
- `getServiceProvidersRequest` - Fetch providers for filter
- `getProvincesRequest` - Fetch provinces for filter
- `getMunicipalitiesRequest` - Fetch municipalities for filter
- `getBarangaysRequest` - Fetch barangays for filter

## Usage Guide

### For Administrators

1. **Accessing the Feature**
   - Navigate to `/admin/cellsites`
   - View all cell sites across regions

2. **Filtering Data**
   - Use the filter dropdowns to narrow results
   - Filters cascade: Province → Municipality → Barangay
   - Clear all filters with one click

3. **Searching**
   - Type in the search box to find specific sites
   - Search works across provider names, coordinates, and site IDs

4. **Managing Multiple Sites**
   - Select sites using checkboxes
   - Use "Update Status" to change status for selected sites
   - Use "Delete Selected" to remove multiple sites (with confirmation)

5. **Navigation**
   - Use pagination controls to browse pages
   - Jump to a specific page using the "Go to" input
   - Adjust page size for your preference

6. **Sharing Views**
   - Copy the URL to share exact filter/search state
   - URLs maintain all settings when shared

### For Developers

#### Adding New Filters

1. Add state variable:
```typescript
const [newFilter, setNewFilter] = useState(() => 
  searchParams.get("newFilter") || "all"
);
```

2. Add to URL update effect:
```typescript
if (newFilter !== "all") params.set("newFilter", newFilter);
```

3. Add to getCells query params:
```typescript
newFilterParam: newFilter !== "all" ? newFilter : undefined,
```

4. Add UI in toolbar component

#### Adding New Batch Operations

1. Create mutation hook
2. Add button/menu item in toolbar
3. Implement handler function
4. Add to toolbar props

## Performance Considerations

- **Server-side pagination**: Only loads current page data
- **Cascading filters**: Dependent filters only fetch when parent is selected
- **Debounced search**: Reduces API calls during typing
- **React Query caching**: Minimizes redundant requests
- **Optimized re-renders**: Proper memoization and state management

## Future Enhancements

- [ ] Detailed cell site view page
- [ ] Export to CSV/Excel
- [ ] Map view integration
- [ ] Advanced analytics dashboard
- [ ] Bulk import functionality
- [ ] Cell site comparison feature
- [ ] Historical data tracking
- [ ] Custom column configuration
- [ ] Saved filter presets

## Accessibility

- Keyboard navigation support
- ARIA labels for screen readers
- Semantic HTML structure
- Color contrast compliance
- Focus indicators

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Maintenance

### Common Tasks

**Updating Filters**:
- Modify `toolbar.tsx` for UI changes
- Update query params in `page.tsx` for logic

**Styling Updates**:
- Colors defined in `columns.tsx` and `toolbar.tsx`
- Use Tailwind classes for consistency

**Adding Columns**:
- Define column in `columns.tsx`
- Follows TanStack Table patterns

## Support

For issues or questions, please refer to the main project README or contact the development team.
