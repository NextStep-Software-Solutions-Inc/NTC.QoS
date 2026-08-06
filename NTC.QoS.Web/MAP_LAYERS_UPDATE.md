# Map Cell Layers Update - Implementation Summary

## Overview
Successfully revamped the `useMapCells` hook to match the modern layer structure used in the CellsiteMapView component, replacing outdated layer IDs and adding proper admin/public separation.

## Key Changes

### 1. New Layer Structure
**Old Layers (Deprecated):**
- `cellular` - Symbol layer with cell icons
- `cellular-radius` - Fill layer for coverage circles
- `cellular-radius-stroke` - Stroke for coverage circles
- `cellular-signal` - Circle layer for signal data
- `cellular-heatmap` - Heatmap for signal strength

**New Layers:**
- `cells-clusters` - Circle layer for clustered cells
- `cells-cluster-count` - Symbol layer showing cluster counts
- `cells-circles` - Circle layer for individual cells (fallback)
- `cells-symbols` - Symbol layer with provider-specific icons
- `cells-heatmap` - Heatmap for cell density (admin-only)
- `cells-point-view` - Small dots for point view mode (admin-only)
- `signal-coverage-circles` - Signal coverage visualization (admin-only)

### 2. Source Changes
- **Old:** `"cellular"` source without clustering
- **New:** `"cells"` source with clustering enabled
- **New:** `"cells-heatmap-source"` for non-clustered admin layers
- **New:** `"signal-coverage-source"` for signal data

### 3. Admin vs Public Separation
**Public Map:**
- ✅ Cell clusters (`cells-clusters`)
- ✅ Cell symbols with provider icons (`cells-symbols`)
- ✅ Individual cell circles (`cells-circles`)
- ❌ Cell heatmap (admin-only)
- ❌ Signal coverage (admin-only)
- ❌ Point view mode (admin-only)

**Admin Map:**
- ✅ All public layers
- ✅ Cell heatmap visualization
- ✅ Signal coverage layer
- ✅ Point view mode (future feature)

### 4. API Changes

#### Before (Legacy):
```typescript
useMapCells({
  cellular: true,
  "cellular-radius": false,
  "cellular-signal": false,
  "cellular-heatmap": false,
});
```

#### After (New):
```typescript
// Public Map
useMapCells(
  {
    cellSites: true,
    cellSiteHeatmap: false,
    signalCoverage: false,
    pointView: false,
  },
  { regionIds: [], isAdminMode: false }
);

// Admin Map
useMapCells(
  {
    cellSites: true,
    cellSiteHeatmap: true,
    signalCoverage: true,
    pointView: false,
  },
  { 
    regionIds: filters.regionIds,
    serviceProviderIds: filters.providers,
    isAdminMode: true 
  }
);
```

### 5. New Utility Module
Created `src/hooks/map-cells-utils.ts` with reusable functions:
- `getClusterLayerPaint()` - Cluster styling
- `getCellCircleLayerPaint()` - Individual cell styling
- `getPointViewCirclePaint()` - Point view styling
- `getCellSiteHeatmapPaint()` - Heatmap configuration
- `getSignalCoverageCirclePaint()` - Signal coverage styling
- `getProviderIconId()` - Icon ID mapping
- `loadProviderIcons()` - Async icon loading

## Benefits

1. **Consistency**: Matches CellsiteMapView implementation
2. **Performance**: Proper clustering reduces render overhead
3. **Security**: Admin-only features properly gated
4. **Maintainability**: Cleaner API with typed interfaces
5. **Extensibility**: Easy to add new layer types

## Migration Notes

### For Public Map Users
No action needed - the map will continue showing cell sites with the new improved clustering.

### For Admin Map Users
The layer controls continue to work as before, but now use the modern layer structure under the hood. The "cellular-radius" and "cellular-signal" toggles now map to the new heatmap and signal coverage layers.

### For Developers
- Old `useMapCells` API with individual boolean flags is supported for backward compatibility
- New API uses `LayerConfig` interface for better type safety
- Admin-only layers require `isAdminMode: true` in options

## Files Modified

1. `src/hooks/useMapCells.tsx` - Completely revamped (768 lines → 500+ lines, cleaner)
2. `src/hooks/map-cells-utils.ts` - New utility module
3. `src/components/Map/PublicViewMap.tsx` - Updated to use new API
4. `src/components/admin-map/AdminViewMap.tsx` - Updated to use new API with admin mode
5. Backup: `src/hooks/useMapCells.tsx.backup` - Original for reference

## Testing Checklist

- [ ] Public map shows clustered cell sites
- [ ] Public map does NOT show heatmap or signal coverage
- [ ] Admin map shows all layers when enabled
- [ ] Layer visibility toggles work correctly
- [ ] Opacity controls work for all layers
- [ ] Provider icons load and display correctly
- [ ] Viewport-based loading works efficiently
- [ ] Provider filtering works correctly
- [ ] Cell selection updates URL with cellId

## Future Enhancements

1. Add point view mode toggle to admin UI
2. Add layer legend for signal strength colors
3. Add animation for cluster expansion
4. Add custom cluster icons per provider
5. Add layer ordering controls
