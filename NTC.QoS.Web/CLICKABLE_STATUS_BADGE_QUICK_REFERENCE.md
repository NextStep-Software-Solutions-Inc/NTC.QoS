# Clickable Status Badge - Quick Reference

## 🎯 Feature Summary
Status badges on cell site cards are now clickable, allowing users to quickly update cell status without entering full edit mode.

---

## 🔧 Implementation Files

### Modified Files
1. **CellsitesTable/table.tsx** - Made status badge clickable
2. **CellsitesManagementPage.tsx** - Updated navigation to support query params
3. **CellsiteDetailPage.tsx** - Added inline status editor

---

## 📝 Key Functions

### 1. getStatusBadge (CellsitesTable/table.tsx)
```typescript
function getStatusBadge(status: string, onClick?: (e: React.MouseEvent) => void)
```
- **Purpose**: Renders status badge with optional click handler
- **Parameters**: 
  - `status`: Cell site status string
  - `onClick`: Optional click handler (makes badge clickable)
- **Returns**: Badge component with status-specific styling

### 2. handleStatusClick (CellsitesTable/table.tsx)
```typescript
const handleStatusClick = useCallback(
  (e: React.MouseEvent) => {
    e.stopPropagation();
    onViewDetails(cell.id, "editStatus=true");
  },
  [cell.id, onViewDetails],
);
```
- **Purpose**: Handles status badge clicks on cards
- **Action**: Navigates to detail page with `?editStatus=true`

### 3. handleStatusChange (CellsiteDetailPage.tsx)
```typescript
const handleStatusChange = useCallback(
  async (newStatus: string) => {
    await updateStatusMutation.mutateAsync({ 
      pathParams: { id: cellId }, 
      payload: newStatus 
    });
    toast.success(`Status updated to ${newStatus}`);
    setEditingStatus(false);
    setTimeout(() => window.location.reload(), 500);
  },
  [updateStatusMutation, cellId],
);
```
- **Purpose**: Updates cell status via API
- **API**: `PATCH /api/cells/:id/status`
- **Side Effects**: Shows toast, closes editor, reloads page

---

## 🎨 Badge States

### Card Badge (List View)
| State | Clickable | Cursor | Hover Effect |
|-------|-----------|--------|--------------|
| Default | ✅ Yes | `pointer` | `opacity-80` |

### Detail Page Badge
| Mode | State | Editable | Component |
|------|-------|----------|-----------|
| View | `editingStatus=false` | ❌ No | Static badge |
| Status Edit | `editingStatus=true` | ✅ Yes | Dropdown + Cancel |
| Full Edit | `isEditing=true` | ✅ Yes | Dropdown inline |

---

## 🚀 Usage Example

### Making a Badge Clickable
```tsx
// ❌ Before
{getStatusBadge(cell.status)}

// ✅ After
{getStatusBadge(cell.status, handleStatusClick)}
```

### Navigation with Query Params
```tsx
// ❌ Before
onViewDetails(cellId);

// ✅ After
onViewDetails(cellId, "editStatus=true");
```

### Handling Query Params
```tsx
const handleViewCell = useCallback(
  (id: string, queryParams?: string) => {
    const url = `/${regionId}/broadband/cellsites/${id}`;
    router.push(queryParams ? `${url}?${queryParams}` : url);
  },
  [router, regionId],
);
```

---

## 🔄 State Management

### Detail Page States
```typescript
// Edit mode states
const [isEditing, setIsEditing] = useState(false);         // Full edit mode
const [editingStatus, setEditingStatus] = useState(false); // Status-only edit

// Auto-trigger from query params
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

---

## 🎯 Status Options

### Valid Values
- `Unknown` - Default state (gray)
- `Pending` - Under review (amber)
- `Licensed` - Approved (emerald)

### API Payload
```typescript
// PATCH /api/cells/:id/status
{
  payload: "Unknown" | "Pending" | "Licensed"
}
```

---

## 🛠️ API Integration

### Endpoint
```
PATCH /api/cells/:id/status
```

### Request
```typescript
{
  pathParams: { id: string },
  payload: string  // "Unknown" | "Pending" | "Licensed"
}
```

### Response
```typescript
// Success: Returns updated cell object
// Error: Throws with error message
```

### Hook Usage
```typescript
const updateStatusMutation = useUpdateCellStatus();

// Call mutation
await updateStatusMutation.mutateAsync({
  pathParams: { id: cellId },
  payload: newStatus
});
```

---

## 🎨 Styling Classes

### Clickable Badge
```tsx
className={`
  ${config.className}
  ${onClick ? 'cursor-pointer hover:opacity-80 transition-opacity' : ''}
`}
```

### Status Dropdown (Status Edit Mode)
```tsx
<SelectTrigger className="w-40 bg-white/20 border-white/30 text-white h-9">
```

### Cancel Button
```tsx
<Button
  variant="ghost"
  size="sm"
  className="text-white/70 hover:text-white h-9">
  <X className="h-4 w-4" />
</Button>
```

---

## ⚡ Event Handling

### Prevent Card Navigation
```typescript
// Stop propagation when clicking status badge
const handleStatusClick = useCallback(
  (e: React.MouseEvent) => {
    e.stopPropagation(); // ← Important!
    onViewDetails(cell.id, "editStatus=true");
  },
  [cell.id, onViewDetails],
);

// Exclude clickable badges from card click
const handleCardClick = useCallback(
  (e: React.MouseEvent) => {
    if (
      (e.target as HTMLElement).closest(".cursor-pointer") // ← Important!
    ) {
      return;
    }
    onViewDetails(cell.id);
  },
  [cell.id, onViewDetails],
);
```

---

## 📊 Component Hierarchy

```
CellsitesManagementPage
├─ CellsitesTable
   ├─ CellCard
      ├─ Status Badge (clickable)
      │  └─ onClick → handleStatusClick
      │     └─ Navigate to detail?editStatus=true
      └─ ...

CellsiteDetailPage (?editStatus=true)
├─ useEffect detects query param
├─ setEditingStatus(true)
└─ Header
   └─ Status Badge Section
      ├─ View Mode: Static badge
      ├─ Status Edit: Dropdown + Cancel
      └─ Full Edit: Dropdown inline
```

---

## 🧪 Testing Commands

### Check Compilation
```bash
pnpm build
```

### Check Types
```bash
pnpm type-check
```

### Lint Code
```bash
pnpm lint
```

---

## 📋 Testing Checklist

### Functional Tests
- [ ] Click status badge on cell card
- [ ] Verify navigation includes `?editStatus=true`
- [ ] Confirm dropdown appears automatically
- [ ] Select different status options
- [ ] Verify toast notification appears
- [ ] Confirm page reloads after update
- [ ] Check updated status displays correctly
- [ ] Test cancel button functionality

### Edge Cases
- [ ] Click status badge during loading
- [ ] Navigate away during status update
- [ ] Test with network errors
- [ ] Try updating without permissions
- [ ] Test with invalid status values

### Integration Tests
- [ ] Card click still navigates to detail page
- [ ] Edit button still triggers full edit mode
- [ ] Status badge not clickable in full edit mode
- [ ] Dropdown shows correct current status
- [ ] Multiple rapid status updates handled correctly

---

## 🐛 Common Issues

### Issue: Badge not clickable
**Solution**: Ensure `onClick` handler is passed to `getStatusBadge()`

### Issue: Card navigates instead of showing dropdown
**Solution**: Check `e.stopPropagation()` in `handleStatusClick`

### Issue: Query param not detected
**Solution**: Verify `useEffect` dependencies include `searchParams`

### Issue: Page doesn't reload after update
**Solution**: Check `setTimeout(() => window.location.reload(), 500)` in `handleStatusChange`

### Issue: Dropdown doesn't show current status
**Solution**: Verify `defaultValue={status}` in Select component

---

## 📖 Related Documentation

- [CLICKABLE_STATUS_BADGE_CHANGELOG.md](./CLICKABLE_STATUS_BADGE_CHANGELOG.md) - Detailed change log
- [CLICKABLE_STATUS_BADGE_VISUAL_GUIDE.md](./CLICKABLE_STATUS_BADGE_VISUAL_GUIDE.md) - Visual flow diagrams
- [Cell.ts](./src/components/features/cell/types/Cell.ts) - Cell type definition
- [editCellStatusRequest.ts](./src/components/features/cell/handlers/editCellStatusRequest.ts) - API handler

---

## 🎯 Quick Actions

### Add Clickable Status Badge to New Table
1. Import `getStatusBadge` function
2. Create `handleStatusClick` callback
3. Pass `onClick` to `getStatusBadge(status, handleStatusClick)`
4. Update parent `onViewDetails` to accept query params

### Add Status Editing to New Detail Page
1. Add `editingStatus` state
2. Detect `editStatus` query param in `useEffect`
3. Render dropdown when `editingStatus=true`
4. Call `handleStatusChange` on selection
5. Add cancel button to exit edit mode

---

**Quick Reference Card v1.0**  
Last Updated: 2025-06-13
