# Barangay Coverage API Update - Implementation Guide

## Overview
This document describes the implementation of the new Barangay Coverage API endpoints with optimistic updates and smart deletion logic.

## Key Changes

### 1. API Endpoint Updates

#### Old Endpoints (Deprecated)
- `PATCH /barangay-coverages/barangays/:barangayId` - Update by barangay ID
- `GET /barangay-coverages/barangays/:barangayId` - Get by barangay ID
- `GET /barangay-coverages/regions/:regionId` - Get by region ID
- `DELETE /barangay-coverages/barangay/:barangayId` - Delete by barangay ID

#### New Endpoints
- `POST /barangay-coverages` - Create coverage (returns coverage ID)
- `PUT /barangay-coverages/:id` - Update coverage by coverage ID (returns 204 No Content)
- `GET /barangay-coverages` - Get all with filters (regionIds, provinceIds, municipalityIds, barangayIds)
- `GET /barangay-coverages/:id` - Get single coverage by coverage ID (includes barangay name/code)
- `DELETE /barangay-coverages/:id` - Delete coverage by coverage ID (returns 204 No Content)

### 2. Type Updates

#### New Types
```typescript
// Lightweight response for list endpoint
export type BarangayCoverageLightDto = {
  id: string;
  barangayId: string;
  wiredFtthPercentage: number;
  wirelessPercentage: number;
  combinedPercentage: number;
  isWiredCovered: boolean;
  isWirelessCovered: boolean;
  updatedAt: string;
  updatedBy: string;
};

// Full response with relationships for single record endpoint
export type BarangayCoverageDto = BarangayCoverageLightDto & {
  barangayName: string;
  barangayCode: string;
};

// Query filters for GET /barangay-coverages
export type BarangayCoverageFilters = {
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
};
```

### 3. Optimistic Updates

The new `useOptimisticBarangayCoverage` hook provides instant UI feedback without waiting for API responses:

```typescript
const {
  saveCoverageOptimistic,
  getOptimisticCoverage,
  optimisticCoverages,
  retryFailedCoverage,
  clearOptimistic,
} = useOptimisticBarangayCoverage();
```

#### Features
- **Instant UI Updates**: Changes appear immediately in the UI
- **No Frequent Refetches**: Avoids calling the large GET /barangay-coverages endpoint repeatedly
- **Error Highlighting**: Failed operations highlight the errored item with a red border
- **Easy Retry**: Save/Retry button appears with barangay name, keeping entered values for easy retry
- **Loading State**: Shows "Saving" with spinner during operations

### 4. Smart Deletion Logic

The system automatically determines when to delete coverage records:

#### Decision Logic
```typescript
const shouldDelete = existingCoverage && wiredPercentage === 0 && wirelessPercentage === 0;
const shouldCreate = !existingCoverage && (wiredPercentage > 0 || wirelessPercentage > 0);
const shouldUpdate = existingCoverage && !shouldDelete;
```

#### Scenarios
1. **No existing coverage + both values = 0**: Do nothing (skip operation)
2. **No existing coverage + any value > 0**: CREATE new coverage
3. **Existing coverage + both values = 0**: DELETE coverage
4. **Existing coverage + any value > 0**: UPDATE coverage

### 5. Fetch Service Enhancement

Updated the fetch service to handle 204 No Content responses:

```typescript
// Handle 204 No Content responses (common for DELETE and PUT operations)
if (res.status === 204 || !contentType) {
  response = Promise.resolve(undefined as R);
}
```

This ensures DELETE and PUT operations (which return 204) work correctly without trying to parse empty response bodies.

## Usage Example

### In ManageBarangayCoverage Component

```typescript
// Initialize the optimistic updates hook
const {
  saveCoverageOptimistic,
  getOptimisticCoverage,
  optimisticCoverages,
} = useOptimisticBarangayCoverage();

// Create a Set of barangay IDs that are currently being saved
const savingBarangays = useMemo(() => {
  const saving = new Set<string>();
  optimisticCoverages.forEach((coverage) => {
    if (coverage.isPending) {
      saving.add(coverage.barangayId);
    }
  });
  return saving;
}, [optimisticCoverages]);

// Handle save (create/update/delete automatically determined)
const handleSave = async (
  barangay: BarangayWithCoverage,
  values: { wiredPercentage: number; wirelessPercentage: number },
) => {
  await saveCoverageOptimistic(
    barangay.id,
    barangay.name,
    barangay.coverage,
    values.wiredPercentage,
    values.wirelessPercentage,
  );
};
```

### Fetching Coverage Data

```typescript
// Fetch coverage data with filters
const { data: coverageData } = useBarangayCoverages({
  pathParams: {},
  queryParams: {
    regionIds: [regionId],
    // Optional: provinceIds, municipalityIds, barangayIds
  },
});
```

## Benefits

1. **Better Performance**: No frequent API calls to large endpoints
2. **Better UX**: Instant feedback with error highlighting and easy retry
3. **Smart Deletion**: Automatically cleans up coverage records when both percentages are 0
4. **Type Safety**: Separate types for list and detail responses
5. **Error Recovery**: Failed operations preserve entered data with visible retry button

## Migration Notes

- All old handlers have been removed or updated
- The component no longer calls `refetchCoverage()` after mutations
- Optimistic updates handle all UI state management
- Failed operations keep modified values visible with error highlighting and retry option
- Action buttons are aligned with barangay name for better layout
- The query key structure has changed to support filters

## Testing Recommendations

1. Test creating coverage for a barangay with no existing coverage
2. Test updating coverage for a barangay with existing coverage
3. Test setting both percentages to 0 (should trigger delete)
4. Test error scenarios to verify error highlighting and retry functionality
5. Test with network delays to see optimistic updates in action
6. Test filtering by region, province, municipality, and barangay IDs
