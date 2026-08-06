# Reports Location Name Update

## Overview
Updated the Connectivity Report and Connectivity Gap Report UI to display complete location names (Barangay, Municipality, Province) instead of just barangay names to avoid user confusion when multiple barangays share the same name.

## Changes Made

### Frontend Changes

#### 1. Type Updates
**File:** `src/components/features/reports/types/Report.ts`
- Added `municipalityName?: string | null` to `AreaDto`
- Added `provinceName?: string | null` to `AreaDto`
- These fields support displaying the complete location hierarchy

#### 2. UI Updates

**File:** `src/components/features/reports/components/ConnectivityReport.tsx`
- Updated the "Areas" tab to display full location names
- Location format: `"Barangay Name, Municipality Name, Province Name"`
- Falls back gracefully if municipality/province names are not provided

**File:** `src/components/features/reports/components/ConnectivityGapReport.tsx`
- Updated the "Priority Areas" tab to display full location names
- Same format as Connectivity Report for consistency

### Backend Changes Required

The backend needs to be updated to include municipality and province names in the `AreaDto` response.

**Affected Endpoints:**
1. `GET /reports/connectivity` - Connectivity Report
2. `GET /reports/connectivity-gap` - Connectivity Gap Report

**Required Changes:**

```csharp
// Update AreaDto class
public class AreaDto
{
    public string LocationId { get; set; }
    public string LocationName { get; set; } // Barangay name
    
    // NEW FIELDS - Add these to support complete location display
    public string? MunicipalityName { get; set; }
    public string? ProvinceName { get; set; }
    
    // ... existing fields ...
    public ConnectivityStatusEnum ConnectivityStatus { get; set; }
    public int? Population { get; set; }
    // ... rest of the fields
}
```

**Database Query Updates:**
The backend queries should be updated to include JOIN operations to fetch municipality and province names when building the AreaDto objects.

Example:
```csharp
var areas = await context.Barangays
    .Include(b => b.Municipality)
        .ThenInclude(m => m.Province)
    .Where(/* your filters */)
    .Select(b => new AreaDto
    {
        LocationId = b.Id,
        LocationName = b.Name,
        MunicipalityName = b.Municipality.Name,  // NEW
        ProvinceName = b.Municipality.Province.Name,  // NEW
        // ... map other fields
    })
    .ToListAsync();
```

## UI Behavior

### With Backend Changes (Recommended)
When the backend includes `municipalityName` and `provinceName`:
- Display: `"Bangcud, San Jose, Nueva Ecija"`
- Display: `"Poblacion, Cabanatuan City, Nueva Ecija"`

### Without Backend Changes (Current Fallback)
If the backend doesn't include these fields (backward compatible):
- Display: `"Bangcud"` (just the barangay name)

## Testing

### Frontend Testing
✅ UI displays complete location names when all fields are provided
✅ UI falls back gracefully when municipality/province names are missing
✅ Consistent formatting across both report types

### Backend Testing Required
After backend implementation:
1. Verify API response includes `municipalityName` and `provinceName`
2. Test with various filter combinations (region, province, municipality)
3. Verify NULL values are handled correctly
4. Test pagination still works correctly

## User Impact

**Before:**
```
Bangcud (Population: 5,771)
Poblacion (Population: 5,442)
Poblacion (Population: 5,252)
```
❌ Confusing - which Poblacion is which?

**After:**
```
Bangcud, San Jose, Nueva Ecija (Population: 5,771)
Poblacion, Cabanatuan City, Nueva Ecija (Population: 5,442)
Poblacion, Gapan City, Nueva Ecija (Population: 5,252)
```
✅ Clear and unambiguous

## Related Files
- Frontend Type: `src/components/features/reports/types/Report.ts`
- Connectivity Report: `src/components/features/reports/components/ConnectivityReport.tsx`
- Connectivity Gap Report: `src/components/features/reports/components/ConnectivityGapReport.tsx`
- Backend DTOs: (To be updated in .NET backend)

## Notes
- This change is backward compatible - the UI will work with or without the new fields
- The backend update is essential for the full user experience improvement
- Consider applying this pattern to other reports that display barangay-level data
