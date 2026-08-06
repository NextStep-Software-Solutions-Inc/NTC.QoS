# Cellsite Map API Changes Required

This document outlines the API changes needed to fully implement the Cellsite Map filtering feature.

## Current API Limitations

The internal `/cells` endpoint currently supports the following query parameters:
- `pageNumber` (int32)
- `pageSize` (int32)
- `search` (string)
- `status` (string)
- `serviceProviderIds` (array of UUID)
- `providerIds` (array of UUID)
- `serviceIds` (array of UUID)
- `longitude` (double)
- `latitude` (double)
- `distance` (double)
- `barangayIds` (array of UUID)
- `municipalityIds` (array of UUID)
- `provinceIds` (array of UUID)
- `regionIds` (array of UUID)

## Missing Filter Parameters

To support advanced filtering in the Cellsite Map (matching requirements for filtering by Generation, Band, and Technology), the following parameters need to be added:

### 1. Generation Filter
```
Parameter: generationIds
Type: array of UUID
Description: Filter cells by cellular technology generation (2G, 3G, 4G, 5G)
Example: ?generationIds=uuid1&generationIds=uuid2
```

**Note:** The `/public/cells` endpoint already supports `generationIds`, but the internal `/cells` endpoint does not.

### 2. Band Filter
```
Parameter: bandIds
Type: array of UUID
Description: Filter cells by frequency band
Example: ?bandIds=uuid1&bandIds=uuid2
```

### 3. Technology Filter
```
Parameter: technologyIds
Type: array of UUID
Description: Filter cells by technology standard (LTE, NR, GSM, UMTS, etc.)
Example: ?technologyIds=uuid1&technologyIds=uuid2
```

## Recommended Backend Changes

### Option A: Update `/cells` endpoint

Add the following parameters to the existing `/cells` GET endpoint:

```csharp
[HttpGet]
public async Task<ActionResult<CellDtoPaginatedResponse>> GetCellsInternal(
    // ... existing parameters ...
    [FromQuery] List<Guid>? generationIds,
    [FromQuery] List<Guid>? bandIds,
    [FromQuery] List<Guid>? technologyIds
)
```

### Option B: Create a new map-optimized endpoint

Create a new endpoint specifically for map views with all filtering capabilities:

```
GET /cells/map
```

With parameters:
- All existing parameters from `/cells`
- `generationIds` (array of UUID)
- `bandIds` (array of UUID)
- `technologyIds` (array of UUID)
- `includeRelations` (boolean) - to control whether to include related data for popups

## Data Model Considerations

For filtering to work correctly, the Cell entity likely needs relationships to:
- **Generation** (via Sector → Network → Technology → Generation)
- **Band** (via Sector → Network → Band relationships)
- **Technology** (via Sector → Network → Technology)

These relationships should be queryable via JOIN filters in the database query.

## Temporary Frontend Implementation

Until the API is updated, the frontend will:
1. Load all cells for the region using infinite pagination
2. Filter locally by provider (using the provider legend)
3. Display a message indicating advanced filtering is not available

## Impact Assessment

- **Low**: Provider filtering works with existing API
- **Medium**: Generation/Band/Technology filtering requires API update
- **Workaround**: Client-side filtering on loaded data (not recommended for large datasets)

## Related Issue

See GitHub Issue #89 for the original feature request.

---

*Document created: December 2024*
*Last updated: December 2024*
