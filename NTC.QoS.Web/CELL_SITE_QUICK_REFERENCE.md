# Cell Site Management - Quick Reference Guide

## 🎯 Quick Overview

All Cell Site operations (Create, Edit, Delete) are now fully functional and aligned with the backend API.

---

## 📍 Key Features

### Create Cell Site ✅
**Location**: Add Cell Site Dialog (in table toolbar)

**Required Fields**:
- ✅ Site Name (max 200 chars)
- ✅ Service Provider (wireless only)
- ✅ Group (auto-filled from provider)
- ✅ Latitude (-90 to 90)
- ✅ Longitude (-180 to 180)
- ✅ Status (default: Unknown)

**Optional Fields**:
- Site ID
- Description
- Address
- CID, LAC, TAC
- Tower Type
- Elevation
- IsVerified (checkbox)

**API**: `POST /cells`

---

### Edit Cell Site ✅
**Location**: Edit dialog (click Edit button on any cell)

**Two Tabs**:

1. **Site Details Tab**
   - CID, LAC, TAC
   - Site ID
   - Tower Type
   - Elevation
   - Latitude, Longitude
   - IsVerified checkbox
   
   **API**: `PATCH /cells/{id}`

2. **Status Tab**
   - Change status: Unknown / Pending / Licensed
   
   **API**: `PATCH /cells/{id}/status`

**Read-only**: Name, Provider, Address (shown in info panel)

---

### Delete Cell Site(s) ✅
**Location**: Delete button or bulk actions

**Features**:
- Single cell deletion
- Bulk deletion (select multiple)
- Confirmation dialog before delete

**API**: `DELETE /cells?ids=[...]`

---

## 🔄 Validation Summary

### Create
```
✓ Name: Required, 1-200 characters
✓ Provider: Required, wireless only
✓ Group: Auto-filled, required
✓ Latitude: Required, -90 to 90
✓ Longitude: Required, -180 to 180
✓ Status: Required, default "Unknown"
```

### Edit
```
✓ Latitude: -90 to 90 (if provided)
✓ Longitude: -180 to 180 (if provided)
✓ All other fields: Optional
```

### Status Update
```
✓ Status: One of ["Unknown", "Pending", "Licensed"]
```

---

## 🎨 UI/UX Highlights

✨ **Auto-fill Magic**: Group automatically populates when you select a provider
✨ **Smart Filtering**: Only wireless providers shown in create form
✨ **Clear Labeling**: Required fields marked with red asterisk (*)
✨ **Helpful Descriptions**: IsVerified has clear explanation
✨ **Tabbed Interface**: Separate sections for details vs status
✨ **In-place Actions**: All operations happen in dialogs (no page navigation)
✨ **Instant Feedback**: Toast notifications for all operations

---

## 📱 How to Use

### Creating a Cell Site:
1. Click "Add Cell Site" button in table toolbar
2. Fill required fields (marked with *)
3. Select a wireless service provider → Group auto-fills
4. Set status (defaults to Unknown)
5. Optionally check "IsVerified" if site is confirmed active
6. Click "Create"

### Editing a Cell Site:
1. Click Edit (pencil icon) on any cell
2. Use "Site Details" tab to update technical fields
3. Use "Status" tab to change operational status
4. Click "Update Details" or "Update Status"

### Deleting Cell Site(s):
1. **Single**: Click Delete (trash icon) on any cell
2. **Bulk**: Select multiple cells → Click "Delete Selected" in batch actions
3. Confirm deletion in dialog

### Verifying a Cell Site:
1. Toggle the verification checkbox on any cell in the table
2. Or use Edit dialog → Site Details → IsVerified checkbox

---

## 🔗 Integration Points

### Table Component
- Displays all cells with filtering/search
- Bulk actions for selected cells
- Individual cell actions (view, edit, delete)
- Embedded AddCellSiteDialog

### Management Page
- Handles all CRUD operations
- Manages dialog state
- Invalidates queries after operations
- Shows success/error toasts

### API Handlers
- `createCellRequest`: POST /cells
- `editCellRequest`: PATCH /cells/{id}
- `editCellStatusRequest`: PATCH /cells/{id}/status
- `deleteCellsRequest`: DELETE /cells

---

## ⚠️ Important Notes

### What You CAN'T Edit:
- ❌ Cell Name
- ❌ Service Provider
- ❌ Group
- ❌ Description
- ❌ Address

These fields are set during creation and cannot be changed later (per API design).

### Status Management:
Status updates use a **separate endpoint** to ensure proper lifecycle management. This is why status has its own tab in the edit dialog.

### IsVerified:
- **Create**: Not sent to API (backend doesn't accept it on create)
- **Edit**: Can be updated via PATCH /cells/{id}
- **Toggle**: Works via table checkbox or edit dialog

---

## 🎯 Testing Scenarios

✅ Create with all required fields
✅ Create with wireless provider (filtering)
✅ Group auto-fills correctly
✅ Edit technical details (CID, LAC, TAC, etc.)
✅ Update status separately
✅ Toggle IsVerified
✅ Delete single cell
✅ Bulk delete multiple cells
✅ Validation errors display correctly
✅ Success toasts appear
✅ Data refreshes after operations

---

## 📞 Need Help?

Check the main change log: `CELL_SITE_MANAGEMENT_FIX.md`

All API endpoints and payloads are documented there.
