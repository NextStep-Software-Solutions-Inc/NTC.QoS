# Cell Site Inline Edit - Quick Reference

## 🎯 Overview
Inline editing feature for Cell Site Detail Page - edit directly on the page without separate dialogs.

---

## 🔄 Edit Flow

```
View Mode → Click "Edit" → Edit Mode → Make Changes → "Save" or "Cancel"
```

### View Mode
- **Buttons:** Edit (gray) | Delete (red)
- **Fields:** Read-only with copy buttons
- **Action:** Click "Edit" to enter edit mode

### Edit Mode
- **Buttons:** Cancel (gray) | Save Changes (blue)
- **Fields:** Editable inputs/selects/checkboxes
- **Actions:** 
  - Click "Cancel" → Resets form, exits edit mode
  - Click "Save Changes" → Submits to API, refetches data

---

## ✏️ Editable Fields

### 📡 Technical Details Card
| Field | Type | Input Type |
|-------|------|------------|
| CID | String | Text input |
| LAC | String | Text input |
| TAC | String | Text input |
| Site ID | String | Text input |
| Tower Type | Enum | Dropdown select |

### 📍 Location Card
| Field | Type | Input Type | Notes |
|-------|------|------------|-------|
| Latitude | Number | Number input | 6 decimal precision |
| Longitude | Number | Number input | 6 decimal precision |
| Elevation | Number | Number input | In meters |

### 📊 Overview Tab
| Field | Type | Input Type |
|-------|------|------------|
| Verified | Boolean | Checkbox |

---

## 🗼 Tower Type Options

```typescript
const towerTypes = [
  "Unknown",
  "Guyed",
  "SelfSupporting",  // Displays as "Self Supporting"
  "Monopole",
  "Stealth",
  "Rooftop",
  "WallMounted",     // Displays as "Wall Mounted"
];
```

---

## 📡 API Endpoint

```
PATCH /cells/:id
```

### Request Payload
```typescript
{
  cid?: string;
  lac?: string;
  tac?: string;
  siteId?: string;
  towerType?: string;
  elevation?: number;
  latitude?: number;
  longitude?: number;
  isVerified?: boolean;
}
```

### Response
```typescript
{
  success: boolean;
  data: Cell;
  message?: string;
}
```

---

## 🎨 UI Components

### Input Styling
- **Width:** 180px (consistent across all inputs)
- **Height:** 32px (h-8)
- **Border:** 2px solid with focus state
- **Font:** Monospace for technical fields

### Button States
- **Edit:** `bg-white/20 hover:bg-white/30`
- **Delete:** `bg-red-500/30 hover:bg-red-500/50`
- **Cancel:** `bg-white/20 hover:bg-white/30`
- **Save:** `bg-blue-500 hover:bg-blue-600` (disabled during mutation)

---

## 💡 Developer Tips

### State Management
```typescript
const [isEditing, setIsEditing] = useState(false);
const [editForm, setEditForm] = useState({
  // All editable fields
});
```

### Form Reset
```typescript
// On cancel or after successful save
setEditForm({
  cid: cell.cid || "",
  lac: cell.lac || "",
  // ... other fields from cell object
});
```

### Save Handler
```typescript
const handleSaveEdit = async () => {
  await updateCellMutation.mutateAsync({
    pathParams: { id: cellId },
    payload: editForm,
  });
  toast.success("Updated successfully");
  setIsEditing(false);
  refetch();
};
```

---

## ✅ Testing Checklist

- [ ] **Edit Button** - Enters edit mode
- [ ] **Cancel Button** - Resets form and exits edit mode
- [ ] **Text Inputs** - CID, LAC, TAC, Site ID editable
- [ ] **Number Inputs** - Lat, Long, Elevation accept numbers
- [ ] **Select Dropdown** - Tower Type shows all 7 options
- [ ] **Checkbox** - Verified toggles properly
- [ ] **Save Button** - Submits changes via PATCH
- [ ] **API Success** - Toast shows, data refetches
- [ ] **API Error** - Toast error displays
- [ ] **Copy Buttons** - Hidden during edit mode
- [ ] **Save Disabled** - Button disabled during API call

---

## 🚨 Common Issues

### Issue: Form doesn't reset on cancel
**Solution:** Check if `handleCancelEdit` is resetting all fields from `cell` object

### Issue: Number inputs show "0" instead of actual value
**Solution:** Ensure `editForm` uses numbers, not strings, for numeric fields

### Issue: Tower Type dropdown shows value instead of label
**Solution:** Verify `selectOptions` array has proper `label` and `value` properties

### Issue: Save button stays disabled
**Solution:** Check mutation loading state and ensure it's not stuck in loading

---

## 📁 File Locations

- **Component:** `src/components/features/cell/pages/CellsiteDetailPage.tsx`
- **API Handler:** `src/components/features/cell/handlers/editCellRequest.ts`
- **Types:** `src/components/features/cell/types/Cell.ts`

---

## 🔗 Related Documentation

- [Cell Site Management Fix](./CELL_SITE_MANAGEMENT_FIX.md)
- [Cell Site Quick Reference](./CELL_SITE_QUICK_REFERENCE.md)
- [Inline Edit Implementation](./CELL_SITE_INLINE_EDIT_IMPLEMENTATION.md)
- [UI Preview](./CELL_SITE_INLINE_EDIT_UI_PREVIEW.html)

---

**Last Updated:** 2025-01-XX  
**Status:** ✅ Production Ready
