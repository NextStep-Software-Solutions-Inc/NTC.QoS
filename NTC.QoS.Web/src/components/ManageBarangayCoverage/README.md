# ManageBarangayCoverage Component

## Overview

This directory contains the refactored barangay coverage management component, organized into smaller, maintainable modules.

## Structure

```
ManageBarangayCoverage/
├── index.tsx              # Main component (orchestrator)
├── types.ts               # Shared TypeScript types
├── BarangayCard.tsx       # Individual barangay card component
├── GroupedView.tsx        # Province > Municipality accordion view
├── FlattenView.tsx        # Flat list view
├── ViewModeToggle.tsx     # Toggle between grouped/flatten views
└── README.md              # This file
```

## Components

### index.tsx (Main Component)

- **Responsibility**: Data fetching, state management, business logic
- **Features**:
  - Infinite scroll pagination (100 items per page)
  - Background coverage data loading
  - Edit state management
  - Mutation handling (create/update)
- **State**:
  - `editingBarangays`: Map of barangays being edited
  - `savingBarangays`: Set of barangays currently saving
  - `viewMode`: Current view mode ("grouped" | "flatten")

### types.ts

- **Exports**:
  - `BarangayWithCoverage`: Barangay with coverage data and UI state
  - `GroupedBarangays`: Hierarchical structure (Province > Municipality > Barangay)
  - `ViewMode`: Type for view mode toggle

### BarangayCard.tsx

- **Responsibility**: Render individual barangay with inline editing
- **Features**:
  - Checkbox for 100% coverage toggle
  - Wired/Wireless percentage inputs
  - Combined coverage display
  - Progress bars
  - Save button (only shown when dirty)
  - Visual states: normal, covered, dirty, saving
- **Memoized**: Yes (prevents unnecessary re-renders)

### GroupedView.tsx

- **Responsibility**: Render barangays grouped by province and municipality
- **Features**:
  - Nested accordions (Province > Municipality > Barangay)
  - Badge counters for barangay counts
  - Icons and color coding

### FlattenView.tsx

- **Responsibility**: Render all barangays in a flat list
- **Features**:
  - Simple vertical list layout
  - Faster rendering for large datasets

### ViewModeToggle.tsx

- **Responsibility**: Toggle between grouped and flatten views
- **Features**:
  - Button group for view mode selection
  - Barangay count display
  - Loading indicator for pagination

## Usage

```tsx
import ManageBarangayCoverage from "@/components/ManageBarangayCoverage";

export default function BarangayCoveragePage() {
  return <ManageBarangayCoverage />;
}
```

## Data Flow

1. **Fetch Barangays**: Infinite query loads 100 barangays per page
2. **Fetch Coverage**: Separate query loads coverage data in background
3. **Merge Data**: useMemo combines barangays with coverage data
4. **Group/Flatten**: useMemo transforms data for current view mode
5. **Render**: View components display data with inline editing
6. **Edit**: State updates tracked in `editingBarangays` Map
7. **Save**: Mutations create/update coverage, refetch data

## Performance Optimizations

- **Infinite Scroll**: Load 100 items at a time instead of 10,000
- **CSS-based View Switching**: Use `hidden` class instead of conditional rendering
- **Memoized Components**: BarangayCard uses React.memo
- **Memoized Data**: useMemo for grouping/flattening operations
- **Background Data Loading**: Coverage data loads independently

## Key Patterns

- **Single Responsibility**: Each component has one clear purpose
- **Prop Drilling**: Minimal - only necessary props passed down
- **State Lifting**: Edit state managed in parent component
- **Optimistic Updates**: UI updates immediately, then sync with server
- **Error Handling**: Toast notifications for success/failure

## Testing Considerations

- Mock `useParams` for region parameter
- Mock React Query hooks for data fetching
- Test infinite scroll with IntersectionObserver mock
- Test view mode switching
- Test edit/save flow with mutations

## Future Enhancements

- [ ] Batch save for multiple barangays
- [ ] Bulk import from CSV/Excel
- [ ] Search/filter barangays
- [ ] Export coverage data
- [ ] Undo/redo for edits
- [ ] Keyboard shortcuts for power users
