# Change Log: Filter Panel Z-Index Update

## Date
2026-01-14

## Issue
Pull the Map Style and Broadband Analytics button behind the Filters & Layers panel

## Initial Misunderstanding
Initially interpreted "behind" as moving the controls inside the panel, but the actual requirement was to make the Filter Panel appear **on top of** these controls (higher z-index).

## Final Solution
Increased the Filter Panel's z-index from `z-40` to `z-60` to ensure it appears as the topmost layer among all map UI controls.

## Implementation Details

### Changes to FilterPanel.tsx
1. **Main expanded panel** (line 195):
   ```tsx
   // Before
   className={cn("fixed top-0 left-0 h-full w-[400px] z-40", "flex flex-col", className)}
   
   // After
   className={cn("fixed top-0 left-0 h-full w-[400px] z-60", "flex flex-col", className)}
   ```

2. **Collapsed panel button** (line 630):
   ```tsx
   // Before
   className={cn(
     "fixed top-1/2 left-0 -translate-y-1/2 ",
     ...
   )}
   
   // After
   className={cn(
     "fixed top-1/2 left-0 -translate-y-1/2 z-60",
     ...
   )}
   ```

### No Changes to AdminViewMap.tsx
The bottom-left controls (Map Style & Analytics button) remain at `z-40`, which is correct - they should be behind the Filter Panel.

## Impact

### When Filter Panel is Expanded
- Filter Panel (400px wide) appears **on top of** Map Style selector and Analytics button
- These controls may be partially or fully covered depending on screen size
- User can still collapse the panel to access the controls

### When Filter Panel is Collapsed
- Only the collapse/expand button is visible (narrow strip on left edge)
- Map Style selector and Analytics button are fully visible and accessible

## Technical Notes

### Z-Index Hierarchy (Final)
```
z-[2147483647] - SearchBar autocomplete
z-[10000]      - SearchBar container
z-60           - Filter Panel (expanded & collapsed) ← NEW TOPMOST UI LAYER
z-50           - Broadband Panel, Cell Details, dropdowns, indicators
z-40           - Map controls (Map Style, Analytics, Navigation, Layers)
z-30           - Attribution text
z-0            - Map container
```

### Design Rationale
- Filter Panel is a primary navigation/configuration element
- It should take precedence over secondary controls when expanded
- Users can quickly collapse it if they need access to covered controls
- The z-60 value ensures it's above all regular controls (z-40) and sidebars (z-50)
- Leaves room for future elements between z-50 and z-60 if needed

## Files Modified
1. `src/components/admin-map/controls/FilterPanel.tsx` (2 lines changed)

## Files Added
1. `ZINDEX_CHANGE_SUMMARY.md` - Technical documentation
2. `ZINDEX_VISUAL_DIAGRAM.md` - Visual diagrams
3. `FILTER_PANEL_ZINDEX_CHANGELOG.md` - This file

## Testing Recommendations
- [ ] Verify Filter Panel expands and collapses smoothly
- [ ] Verify Filter Panel appears above Map Style when expanded
- [ ] Verify Filter Panel appears above Analytics button when expanded
- [ ] Verify collapsed button is always clickable
- [ ] Test on mobile, tablet, and desktop screen sizes
- [ ] Verify no interference with SearchBar autocomplete
- [ ] Verify no interference with right-side panels (Cell Details, Analytics)

## Minimal Change Principle
This solution follows the minimal change principle:
- Only 2 lines modified in 1 file
- No structural changes to components
- No changes to functionality or behavior
- Simple, clear, and maintainable
- Well-documented for future reference
