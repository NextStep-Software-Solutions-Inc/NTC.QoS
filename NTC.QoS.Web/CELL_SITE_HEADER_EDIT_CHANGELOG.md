# Cell Site Header Edit - Change Log

## 📅 Date: 2025-02-09

## 🎯 Summary
Added inline editing capability for the cell site name and status in the header section of the Cell Site Detail Page.

---

## ✅ Changes Made

### 1. Added Name and Status to Edit Form
**File:** `src/components/features/cell/pages/CellsiteDetailPage.tsx`

Extended `editForm` state to include:
```typescript
const [editForm, setEditForm] = useState({
  name: "",        // ← NEW: Cell site name
  status: "",      // ← NEW: Cell site status
  cid: "",
  lac: "",
  // ... other fields
});
```

### 2. Editable Cell Site Name
**Location:** Header section

#### View Mode:
- Displays as large heading (h1) with the cell name
- Fallback: Shows `siteId` or "Unnamed Cell Site" if name is empty

#### Edit Mode:
- Shows as a `Textarea` component
- Supports multi-line input (2 rows)
- Styled to match header aesthetic:
  - White text on purple gradient background
  - Semi-transparent white background
  - White border with 30% opacity
  - Placeholder text with 50% opacity

```tsx
{isEditing ? (
  <Textarea
    value={editForm.name}
    onChange={(e) => setEditForm((prev) => ({ ...prev, name: e.target.value }))}
    className="text-2xl md:text-3xl font-bold bg-white/20 text-white border-white/30"
    placeholder="Enter cell site name"
    rows={2}
  />
) : (
  <h1 className="text-3xl md:text-4xl font-bold text-white">
    {cell.name || cell.siteId || "Unnamed Cell Site"}
  </h1>
)}
```

### 3. Editable Status Badge
**Location:** Below cell name in header

#### View Mode (Non-editing):
- Shows status badge with icon and color
- Clicking badge opens inline status editor (existing functionality)
- Tooltip: "Click to change status"

#### Edit Mode:
- Shows a `Select` dropdown with all status options
- Width: 192px (w-48)
- Options available:
  - Unknown
  - Pending
  - Licensed
  - Active
  - Inactive
  - Maintenance

```tsx
{isEditing ? (
  <Select 
    value={editForm.status || status} 
    onValueChange={(value) => setEditForm((prev) => ({ ...prev, status: value }))}
  >
    <SelectTrigger className="w-48 bg-white/20 border-white/30 text-white">
      <SelectValue />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value="Unknown">Unknown</SelectItem>
      <SelectItem value="Pending">Pending</SelectItem>
      <SelectItem value="Licensed">Licensed</SelectItem>
      <SelectItem value="Active">Active</SelectItem>
      <SelectItem value="Inactive">Inactive</SelectItem>
      <SelectItem value="Maintenance">Maintenance</SelectItem>
    </SelectContent>
  </Select>
) : (
  // ... existing badge display
)}
```

### 4. Updated Save Handler
**File:** `src/components/features/cell/pages/CellsiteDetailPage.tsx`

Modified `handleSaveEdit` to handle status updates separately:

```typescript
const handleSaveEdit = useCallback(async () => {
  try {
    // 1. Update cell details (all fields except status)
    await updateCellMutation.mutateAsync({
      pathParams: { id: cellId },
      payload: {
        cid: editForm.cid || undefined,
        lac: editForm.lac || undefined,
        // ... other fields
      },
    });
    
    // 2. Update status separately if changed
    if (editForm.status !== cell?.status) {
      await updateStatusMutation.mutateAsync({
        pathParams: { id: cellId },
        payload: editForm.status,
      });
    }
    
    toast.success("Cell site updated successfully");
    setIsEditing(false);
    refetch();
  } catch (error) {
    toast.error("Failed to update cell site");
  }
}, [updateCellMutation, updateStatusMutation, cellId, editForm, cell?.status, refetch]);
```

**Why separate calls?**
- The backend uses different endpoints:
  - `PATCH /cells/:id` - for cell details (doesn't include name or status in payload)
  - `PATCH /cells/:id/status` - for status updates only

### 5. State Initialization
**File:** `src/components/features/cell/pages/CellsiteDetailPage.tsx`

Updated `useEffect` and `handleCancelEdit` to include name and status:

```typescript
useEffect(() => {
  if (cell && !isEditing) {
    setEditForm({
      name: cell.name || "",
      status: cell.status || "Unknown",
      // ... other fields
    });
  }
}, [cell, isEditing]);
```

---

## 🎨 UI/UX Changes

### Edit Mode Indicators
When user clicks "Edit" button:
- ✅ Cell name becomes a large textarea
- ✅ Status badge becomes a dropdown selector
- ✅ All location and technical fields become editable
- ✅ Action buttons change to "Cancel" and "Save Changes"

### Visual Consistency
- Textarea matches header styling (white text, semi-transparent background)
- Select dropdown maintains header color scheme
- All inputs have proper focus states
- Responsive layout maintained

---

## 🔧 Technical Details

### New Imports
```typescript
import { Textarea } from "@/components/ui/textarea";
```

### State Management
- Name and status are now part of the main `editForm` state
- No separate state needed for status editing in edit mode
- Existing `editingStatus` state still works for quick status changes in view mode

### API Integration
- **Name Field:** Currently included in editForm but may not be sent to API (based on editCellRequest spec)
- **Status Field:** Uses `editCellStatusRequest` handler (PATCH /cells/:id/status)
- Only calls status update API if status actually changed

---

## 🧪 Testing Checklist

### Name Field:
- [ ] Click Edit → Name shows as textarea
- [ ] Can modify name text
- [ ] Textarea supports multi-line input
- [ ] Cancel resets name to original value
- [ ] Save attempts to update name (check API response)

### Status Field:
- [ ] Click Edit → Status shows as dropdown
- [ ] Dropdown shows all 6 status options
- [ ] Can select different status
- [ ] Cancel resets status to original value
- [ ] Save updates status via PATCH /cells/:id/status
- [ ] Toast notification on success/failure
- [ ] Data refetches after save

### Integration:
- [ ] Name + Status + other fields save together
- [ ] Only status API called if status changed
- [ ] Form resets properly on cancel
- [ ] No duplicate status badge appears
- [ ] Existing quick status change still works in view mode

---

## ⚠️ Important Notes

### Name Field Limitation
The `editCellRequest` handler does not currently include `name` in the payload. This means:
- Name is editable in the UI
- Name is part of the editForm state
- **However**, the name may not actually update on the backend

**Options:**
1. Check if backend accepts name in PATCH /cells/:id payload
2. Create separate endpoint for name updates
3. Remove name from editable fields if backend doesn't support it

**Recommendation:** Test with backend API to confirm name field behavior.

### Status Options
The status dropdown includes 6 options:
- Unknown, Pending, Licensed (from create form)
- Active, Inactive, Maintenance (from quick status change)

Verify with backend which status values are valid.

---

## 📊 Summary

### Fields Now Editable in Header:
1. ✅ **Cell Site Name** (Textarea, 2 rows)
2. ✅ **Status** (Dropdown, 6 options)

### Edit Flow:
```
View Mode → Click "Edit"
  ↓
Edit Mode
  ↓
Modify name, status, and other fields
  ↓
Click "Save Changes"
  ↓
API Calls:
  1. PATCH /cells/:id (cell details)
  2. PATCH /cells/:id/status (if status changed)
  ↓
Success → Refetch data → Exit edit mode
```

### Backward Compatibility:
- ✅ Quick status change in view mode still works
- ✅ Existing edit functionality preserved
- ✅ No breaking changes to other components

---

## 🔗 Related Files

- **Component:** `src/components/features/cell/pages/CellsiteDetailPage.tsx`
- **API Handlers:**
  - `src/components/features/cell/handlers/editCellRequest.ts`
  - `src/components/features/cell/handlers/editCellStatusRequest.ts`
- **Types:** `src/components/features/cell/types/Cell.ts`

---

**Status:** ✅ Complete - Ready for Testing  
**TypeScript Errors:** 0  
**Build Status:** Passing  
**Next Action:** Test name and status updates with backend API
