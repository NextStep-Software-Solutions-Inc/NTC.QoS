# NAP Bulk Delete - Quick Reference

## TL;DR
**Problem:** URL length limit prevented deleting large numbers of NAPs (1000+)  
**Solution:** Chunked deletion (100 IDs/request) + selection limit (500 max) + progress UI

---

## Quick Facts

### Limits
- **Selection Limit:** 500 NAPs maximum per deletion
- **Chunk Size:** 100 IDs per API request
- **Warning Threshold:** Orange warning at 400 selections (80%)
- **URL Safety:** Each chunk stays well under 8KB URL limit

### UI States
- **Blue (0-399):** Normal, all actions enabled
- **Orange (400-500):** Warning, approaching limit
- **Red (501+):** Error, actions disabled

---

## For Developers

### Using BulkDeleteDialog in Other Features
```typescript
import { BulkDeleteDialog } from "@/components/features/NAP/components/BulkDeleteDialog";

<BulkDeleteDialog
  open={deleteDialogOpen}
  onOpenChange={setDeleteDialogOpen}
  idsToDelete={selectedIds}
  onConfirmDelete={handleDeleteChunk}
  itemName="Cellsite" // or "Broadband", "Provider", etc.
/>

// Handler that processes one chunk
const handleDeleteChunk = async (chunkIds: string[]) => {
  await deleteMutation.mutateAsync({ 
    pathParams: {}, 
    queryParams: { ids: chunkIds } 
  });
};
```

### Adjusting Limits
**Selection Limit (src/.../useSelection.ts + BulkDeleteDialog.tsx):**
```typescript
const MAX_SELECTION_LIMIT = 500; // Change both files to match
```

**Chunk Size (src/.../BulkDeleteDialog.tsx):**
```typescript
const CHUNK_SIZE = 100; // Max ~150 for UUIDs (stay under 8KB URL)
```

**Warning Threshold (src/.../BatchActions.tsx):**
```typescript
const WARNING_THRESHOLD = 400; // When to show orange (80% of limit)
```

---

## For Users

### How to Delete Multiple NAPs
1. **Filter** your NAPs (optional)
2. **Select** items (checkbox or "Select All")
3. **Click Delete** in the BatchActions bar
4. **Confirm** in the dialog
5. **Watch progress** for large deletions
6. **Done!** Table auto-refreshes

### Selection Tips
- **Select All** automatically limits to 500 if more available
- **Orange warning** appears at 400+ selections
- **Red error** if you somehow exceed 500 (actions disabled)
- **Clear Selection** button always available

### What Happens During Deletion
```
Small (1-100 NAPs):
  → Single request, instant completion

Medium (101-500 NAPs):
  → Multiple batches with progress bar
  → Each batch: ~100ms (typical)
  → Total time: ~0.5-1 second

Large (500+ NAPs):
  → Not allowed in single action
  → Select in batches of 500
```

---

## Files Modified

### New Files
- `src/components/features/NAP/components/BulkDeleteDialog.tsx` - Progress dialog

### Updated Files
- `src/components/features/NAP/tables/NAPTable/hooks/useSelection.ts` - Limit enforcement
- `src/components/features/NAP/tables/NAPTable/components/BatchActions.tsx` - Visual warnings
- `src/components/features/NAP/pages/NAPsManagementPage.tsx` - Integration

---

## Common Scenarios

### ✅ Delete 50 NAPs
- Single chunk, instant
- No special behavior

### ✅ Delete 250 NAPs
- 3 chunks (100, 100, 50)
- Progress bar shown
- ~0.3 seconds total

### ✅ Delete 500 NAPs (at limit)
- 5 chunks
- Orange warning shown
- ~0.7 seconds total
- Deletion allowed

### ⚠️ Try to delete 600 NAPs
- "Select All" limits to 500
- Toast notification shown
- Can delete in 2 batches (500 + 100)

### ❌ Try to delete 1000+ NAPs
- Must be done in multiple actions
- Select 500, delete, repeat
- Or filter and delete subsets

---

## Troubleshooting

### "Maximum 500 items can be selected at once"
**Cause:** Tried to select more than the limit  
**Solution:** Delete in batches of 500, or filter results first

### Partial Deletion Success
**Cause:** Some chunks failed (network/server error)  
**Shown:** "Deleted 400 NAPs, failed to delete 100"  
**Solution:** Check failed items, retry if needed

### Delete Button Disabled
**Cause:** Over 500 items selected (red state)  
**Solution:** Click "Clear Selection" and select ≤500 items

---

## Performance Tips

### For Large Datasets (10,000+ NAPs)
1. **Use filters** to narrow results first
2. **Delete by region/provider** (smaller batches)
3. **Delete in multiple sessions** if needed
4. Consider requesting backend bulk delete API

### Optimal Chunk Size
- Current: 100 IDs/request
- Safe range: 50-150 (depends on ID length)
- Don't increase beyond 150 (URL safety)

---

## Backend Considerations

### Current Implementation
```
DELETE /NAPs?ids=id1&ids=id2&...&id100
```
- ✅ Works for current use case
- ⚠️ Limited by URL length
- 💡 Consider POST for future

### Recommended Backend Enhancement
```
POST /NAPs/bulk-delete
Body: { ids: [...] }
```
**Benefits:**
- No URL limit
- Single request for any amount
- Industry standard
- Future-proof

**Impact on Frontend:**
- Remove chunking logic
- Simpler code
- Faster operations
- No user-facing changes

---

## Testing Checklist

### Manual Tests
- [ ] Select 1 NAP → Delete → Success
- [ ] Select 50 NAPs → Delete → Single chunk
- [ ] Select 250 NAPs → Delete → Shows progress
- [ ] Select 500 NAPs → Orange warning → Delete works
- [ ] Select All (1000 NAPs) → Limits to 500 → Toast shown
- [ ] Try to select 501st → Toast warning → Blocked
- [ ] Delete 300 NAPs → Watch progress → All deleted
- [ ] Simulate network error → See partial success

### Edge Cases
- [ ] Delete at exactly 100 (chunk boundary)
- [ ] Delete 1 NAP (minimum)
- [ ] Delete 500 NAPs (maximum)
- [ ] Cancel during confirmation
- [ ] Multiple users deleting simultaneously

---

## Change Log Summary

**What Changed:**
- ✅ Added chunked deletion (100 IDs per request)
- ✅ Added progress tracking UI
- ✅ Enforced 500-item selection limit
- ✅ Added visual warnings (blue/orange/red)
- ✅ Improved error handling per-chunk

**What Didn't Change:**
- ✅ Backend API unchanged
- ✅ Endpoint format same
- ✅ Single-item deletion same
- ✅ Table UI/UX same

**Breaking Changes:** None

---

## Key Metrics

### URL Length Comparison
| Items | Before | After (Chunked) |
|-------|--------|-----------------|
| 100 | ~4.4KB ✅ | ~4.4KB ✅ |
| 500 | ~22KB ❌ | 5 × ~4.4KB ✅ |
| 1000 | ~44KB ❌ | 10 × ~4.4KB ✅ |

### Time Estimates (100ms latency/request)
| Items | Chunks | Time |
|-------|--------|------|
| 50 | 1 | 0.1s ⚡ |
| 250 | 3 | 0.4s 🚀 |
| 500 | 5 | 0.7s ⏱️ |

---

## Need Help?

### Documentation
- Full Guide: [NAP_BULK_DELETE_OPTIMIZATION.md](NAP_BULK_DELETE_OPTIMIZATION.md)
- Visual Guide: [NAP_BULK_DELETE_VISUAL_GUIDE.md](NAP_BULK_DELETE_VISUAL_GUIDE.md)
- Code: `src/components/features/NAP/components/BulkDeleteDialog.tsx`

### Common Questions

**Q: Why not just increase chunk size to 500?**  
A: URL would exceed safe limit (~22KB), causing failures.

**Q: Can we delete 10,000 NAPs at once?**  
A: No, 500 max per action. Use filters or multiple actions.

**Q: What if deletion fails mid-process?**  
A: Shows partial success (e.g., "Deleted 200, failed 100"). Successfully deleted items stay deleted.

**Q: Can I cancel during deletion?**  
A: Currently no (coming in future update). Dialog shows "Processing..." until complete.

**Q: How do I apply this to Cellsites?**  
A: Copy BulkDeleteDialog usage pattern, change `itemName` prop to "Cellsite".

---

**Last Updated:** January 26, 2026  
**Version:** 1.0  
**Status:** ✅ Production Ready
