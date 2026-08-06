# Clickable Status Badge Implementation - Change Log

## Summary
Implemented clickable status badges on cell site cards that navigate to the detail page with an isolated status editing mode. This allows users to quickly update cell site status without entering full edit mode.

## User Experience Flow
1. **On Cell Cards**: User clicks on the status badge (e.g., "Unknown", "Pending", "Licensed")
2. **Navigation**: Browser navigates to cell site detail page with `?editStatus=true` query parameter
3. **Inline Status Editor**: Detail page header automatically shows status dropdown with cancel button
4. **Update**: User selects new status from dropdown (Unknown/Pending/Licensed)
5. **Confirmation**: Toast notification confirms successful update
6. **Reload**: Page reloads automatically to show updated status

## Changes Made

### 1. CellsitesTable/table.tsx
**File**: `src/components/features/cell/tables/CellsitesTable/table.tsx`

#### Updated getStatusBadge Function
```typescript
// Before
function getStatusBadge(status: string) {
  // Returns static badge
}

// After
function getStatusBadge(status: string, onClick?: (e: React.MouseEvent) => void) {
  return (
    <Badge 
      className={`${config.className} ${onClick ? 'cursor-pointer hover:opacity-80 transition-opacity' : ''}`}
      onClick={onClick}>
      <span className="mr-1">{config.icon}</span>
      {status || "Unknown"}
    </Badge>
  );
}
```
**Changes**:
- Added optional `onClick` parameter
- Added `cursor-pointer` and hover effect when clickable
- Preserves existing status styling

#### Added handleStatusClick Handler
```typescript
const handleStatusClick = useCallback(
  (e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent card navigation
    onViewDetails(cell.id, "editStatus=true");
  },
  [cell.id, onViewDetails],
);
```
**Purpose**: 
- Stops event propagation to prevent card click
- Navigates to detail page with `editStatus=true` query param

#### Updated Badge Rendering
```typescript
// Before
{getStatusBadge(cell.status)}

// After
{getStatusBadge(cell.status, handleStatusClick)}
```

#### Updated CellCardProps Interface
```typescript
interface CellCardProps {
  // ...
  onViewDetails: (id: string, queryParams?: string) => void; // Added queryParams param
  // ...
}
```

#### Updated handleCardClick
```typescript
const handleCardClick = useCallback(
  (e: React.MouseEvent) => {
    if (
      // ... existing checks
      (e.target as HTMLElement).closest(".cursor-pointer") // Added check for clickable badges
    ) {
      return;
    }
    onViewDetails(cell.id);
  },
  [cell.id, onViewDetails],
);
```
**Purpose**: Prevents card navigation when clicking on clickable status badge

---

### 2. CellsitesManagementPage.tsx
**File**: `src/components/features/cell/pages/CellsitesManagementPage.tsx`

#### Updated handleViewCell
```typescript
// Before
const handleViewCell = useCallback(
  (id: string) => {
    router.push(`/${regionId}/broadband/cellsites/${id}`);
  },
  [router, regionId],
);

// After
const handleViewCell = useCallback(
  (id: string, queryParams?: string) => {
    const url = `/${regionId}/broadband/cellsites/${id}`;
    router.push(queryParams ? `${url}?${queryParams}` : url);
  },
  [router, regionId],
);
```
**Changes**:
- Accepts optional `queryParams` string
- Appends query params to URL if provided

---

### 3. CellsiteDetailPage.tsx
**File**: `src/components/features/cell/pages/CellsiteDetailPage.tsx`

#### Added editingStatus State
```typescript
const [editingStatus, setEditingStatus] = useState(false);
```

#### Updated useEffect for Auto-Edit Triggers
```typescript
// Before
React.useEffect(() => {
  const editParam = searchParams.get("edit");
  if (editParam === "true" && cell && !isEditing) {
    setIsEditing(true);
  }
}, [searchParams, cell, isEditing]);

// After
React.useEffect(() => {
  const editParam = searchParams.get("edit");
  const editStatusParam = searchParams.get("editStatus");
  if (editParam === "true" && cell && !isEditing) {
    setIsEditing(true);
  }
  if (editStatusParam === "true" && cell && !editingStatus) {
    setEditingStatus(true);
  }
}, [searchParams, cell, isEditing, editingStatus]);
```
**Changes**:
- Detects `editStatus=true` query parameter
- Automatically sets `editingStatus` to true

#### Added handleStatusChange
```typescript
const handleStatusChange = useCallback(
  async (newStatus: string) => {
    try {
      await updateStatusMutation.mutateAsync({ 
        pathParams: { id: cellId }, 
        payload: newStatus 
      });
      toast.success(`Status updated to ${newStatus}`);
      setEditingStatus(false);
      // Reload browser to display updates
      setTimeout(() => {
        window.location.reload();
      }, 500);
    } catch (error) {
      toast.error("Failed to update status");
      console.error("Status update error:", error);
    }
  },
  [updateStatusMutation, cellId],
);
```
**Purpose**:
- Calls `PATCH /cells/:id/status` endpoint
- Shows success/error toast notifications
- Closes status editor
- Reloads page after 500ms to show updated data

#### Updated Status Badge Section
```typescript
{/* Status badge */}
{isEditing ? (
  // Full edit mode: Status dropdown inline
  <Select 
    value={editForm.status || status} 
    onValueChange={(value) => setEditForm((prev) => ({ ...prev, status: value }))}>
    <SelectTrigger className="w-48 bg-white/20 border-white/30 text-white">
      <SelectValue />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value="Unknown">Unknown</SelectItem>
      <SelectItem value="Pending">Pending</SelectItem>
      <SelectItem value="Licensed">Licensed</SelectItem>
    </SelectContent>
  </Select>
) : editingStatus ? (
  // Status-only edit mode: Dropdown with cancel button
  <div className="flex items-center gap-2">
    <Select defaultValue={status} onValueChange={handleStatusChange}>
      <SelectTrigger className="w-40 bg-white/20 border-white/30 text-white h-9">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="Unknown">Unknown</SelectItem>
        <SelectItem value="Pending">Pending</SelectItem>
        <SelectItem value="Licensed">Licensed</SelectItem>
      </SelectContent>
    </Select>
    <Button
      variant="ghost"
      size="sm"
      onClick={() => setEditingStatus(false)}
      className="text-white/70 hover:text-white h-9">
      <X className="h-4 w-4" />
    </Button>
  </div>
) : (
  // View mode: Static badge
  <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full ${statusStyle.bg} ${statusStyle.text} font-medium text-sm border ${statusStyle.border || "border-white/20"}`}>
    <StatusIcon className="h-4 w-4" />
    {statusStyle.label}
  </div>
)}
```

**Three Modes**:
1. **Full Edit Mode** (`isEditing=true`): Status dropdown inline with all other fields
2. **Status-Only Edit** (`editingStatus=true`): Compact dropdown with cancel button
3. **View Mode**: Static badge display

---

## Technical Details

### API Integration
- **Endpoint**: `PATCH /api/cells/:id/status`
- **Handler**: `editCellStatusRequest.ts`
- **Hook**: `useUpdateCellStatus` (from React Query)
- **Payload**: Simple string value ("Unknown" | "Pending" | "Licensed")

### Status Options
Limited to three valid cell site statuses:
- **Unknown**: Default state (gray badge)
- **Pending**: Under review (amber badge)
- **Licensed**: Approved for operation (emerald badge)

### Design Patterns
- **Event Propagation**: `e.stopPropagation()` prevents conflicting click handlers
- **Query Parameters**: Uses `?editStatus=true` for clean state management
- **Inline Editing**: Contextual editor appears in-place without navigation
- **Auto-reload**: Ensures UI reflects updated backend data

### Styling
- Status badge inherits existing statusConfig colors
- Hover effect: `opacity-80` on badge when clickable
- Cursor changes to pointer when hovering over clickable badge
- Cancel button (X icon) aligns with dropdown in status-only edit mode

---

## Testing Checklist
- [ ] Click status badge on cell card
- [ ] Verify navigation to detail page with `?editStatus=true`
- [ ] Confirm status dropdown appears in header
- [ ] Select new status (Unknown/Pending/Licensed)
- [ ] Verify toast notification shows success message
- [ ] Confirm page reloads after 500ms
- [ ] Check updated status displays correctly
- [ ] Test cancel button (X) to exit status editing
- [ ] Verify card click still navigates to detail page
- [ ] Test full edit mode still works (Edit button)
- [ ] Confirm status badge not clickable in full edit mode

---

## Benefits
1. **Quick Updates**: Users can update status without entering full edit mode
2. **Better UX**: Contextual editing reduces friction
3. **Clear Intent**: Separate status editing prevents accidental changes to other fields
4. **Consistent Design**: Reuses existing badge styling and dropdown components
5. **Navigation Flow**: Natural progression from card → detail page → inline edit

---

## Future Enhancements
- Add optimistic updates (update UI before API response)
- Add loading spinner during status update
- Consider making verified badge clickable as well
- Add keyboard shortcuts (e.g., Escape to cancel)
- Add status change history/audit log

---

**Date**: 2025-06-13  
**Author**: GitHub Copilot  
**Status**: ✅ Complete
