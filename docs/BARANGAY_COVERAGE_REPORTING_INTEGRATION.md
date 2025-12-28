# Barangay Coverage Reporting Integration - Summary

## Overview
This document describes the breaking changes made to reporting logic to scope reports to only barangays with BarangayCoverage records.

---

## Changes Made

### Report Handlers Updated

#### 1. Wireless Broadband Reports

**Files Modified:**
- `GetWirelessBroadbandReportHandler.cs`
- `GetWirelessBroadbandScoredReportQueryHandler.cs`

**Changes:**
- Added `IBarangayCoverageRepository` dependency injection
- Modified barangay queries to filter by BarangayCoverage presence
- Applied filter at three geographic levels:
  - Municipality level
  - Province level  
  - Region level

**Before:**
```csharp
var barangayQuery = _barangayRepository.Query(b => b.MunicipalityId == request.MunicipalityId)
    .OrderBy(b => b.Name)
    .Select(b => new { b.Id, b.Name, MunicipalityName = b.Municipality.Name, b.Boundaries });
```

**After:**
```csharp
// Only include barangays with BarangayCoverage records
var coveredBarangayIds = _barangayCoverageRepository.Query()
    .Select(bc => bc.BarangayId);

var barangayQuery = _barangayRepository.Query(b => 
        b.MunicipalityId == request.MunicipalityId && 
        coveredBarangayIds.Contains(b.Id))
    .OrderBy(b => b.Name)
    .Select(b => new { b.Id, b.Name, MunicipalityName = b.Municipality.Name, b.Boundaries });
```

#### 2. Wired Broadband Reports

**File Modified:**
- `GetWiredBroadbandReportQueryHandler.cs`

**Changes:**
- Added `IBarangayCoverageRepository` dependency injection
- Modified barangay query within municipality processing to filter by BarangayCoverage

**Before:**
```csharp
var barangays = await _barangayRepository.ToListAsync(
    _barangayRepository.QueryAsNoTracking(b => b.MunicipalityId == municipalityId)
        .Select(b => new { b.Id, b.Name, b.Code })
        .OrderBy(b => b.Name),
    cancellationToken);
```

**After:**
```csharp
// Only include barangays with BarangayCoverage records
var coveredBarangayIds = _barangayCoverageRepository.Query()
    .Select(bc => bc.BarangayId);

var barangays = await _barangayRepository.ToListAsync(
    _barangayRepository.QueryAsNoTracking(b => 
            b.MunicipalityId == municipalityId && 
            coveredBarangayIds.Contains(b.Id))
        .Select(b => new { b.Id, b.Name, b.Code })
        .OrderBy(b => b.Name),
    cancellationToken);
```

---

## Impact Analysis

### Breaking Change
⚠️ **This is a BREAKING CHANGE** - Reports will now show **fewer barangays** than before.

**Before:** Reports included all 42,000+ barangays in the system
**After:** Reports include only barangays with BarangayCoverage records

### User Impact

1. **Regional Admins**
   - Must explicitly create BarangayCoverage entries for barangays they manage
   - Barangays without coverage entries will not appear in reports
   - Provides explicit control over which barangays are "in scope"

2. **Report Users**
   - May see fewer barangays in reports after deployment
   - Report counts will be more accurate (only tracked barangays)
   - Clearer picture of actual coverage management scope

3. **Data Migration Required**
   - Before production deployment, existing tracked barangays should have BarangayCoverage records created
   - Consider bulk import/seeding script for initial data population

---

## Query Performance

### Optimization Strategy
The implementation uses a subquery pattern for efficient filtering:

```csharp
var coveredBarangayIds = _barangayCoverageRepository.Query()
    .Select(bc => bc.BarangayId);

// Then used in WHERE clause
.Where(b => coveredBarangayIds.Contains(b.Id))
```

**Benefits:**
- Database-side filtering using SQL EXISTS/IN clause
- No need to materialize BarangayCoverage records in memory
- Leverages existing indexes on BarangayId
- Minimal overhead on report generation

**SQL Generated (approximate):**
```sql
SELECT b.* FROM "Barangays" b
WHERE b."MunicipalityId" = @municipalityId
  AND b."Id" IN (
    SELECT bc."BarangayId" FROM "BarangayCoverages" bc WHERE bc."IsDeleted" = FALSE
  )
ORDER BY b."Name"
```

---

## Testing Recommendations

### 1. Unit Tests
- Test that reports filter correctly by BarangayCoverage
- Test with empty BarangayCoverage table (should return no barangays)
- Test with partial coverage (should return only covered barangays)

### 2. Integration Tests
- Generate reports at municipality, province, and region levels
- Verify barangay counts match BarangayCoverage record counts
- Test pagination still works correctly with filtered results

### 3. Performance Tests
- Measure report generation time before/after changes
- Test with large datasets (5000+ barangays)
- Monitor database query execution plans

---

## Migration Guide

### For Development/Staging

1. **Seed Sample Data:**
```sql
-- Create coverage for all barangays in a test municipality
INSERT INTO "BarangayCoverages" ("Id", "BarangayId", "WiredFtthPercentage", "WirelessPercentage", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
SELECT 
    gen_random_uuid(),
    b."Id",
    50.0,  -- Sample wired percentage
    75.0,  -- Sample wireless percentage
    NOW(),
    'system-migration',
    NOW(),
    'system-migration',
    FALSE
FROM "Barangays" b
WHERE b."MunicipalityId" = 'YOUR-TEST-MUNICIPALITY-ID';
```

2. **Generate Test Reports:**
- Run wireless broadband reports
- Run wired broadband reports
- Verify only barangays with coverage appear

### For Production

1. **Pre-Deployment Analysis:**
```sql
-- Count barangays currently tracked (adjust criteria as needed)
SELECT COUNT(*) as tracked_barangays
FROM "Barangays" b
WHERE EXISTS (
  -- Add your criteria for "tracked" barangays
  -- Examples: has connectivity assessments, has speedtests, etc.
  SELECT 1 FROM "ConnectivityAssessments" ca 
  WHERE ca."LocationId" = b."Id" AND ca."LocationType" = 'Barangay'
);
```

2. **Create Migration Script:**
```sql
-- Example: Create coverage for barangays with connectivity assessments
INSERT INTO "BarangayCoverages" (
    "Id", "BarangayId", "WiredFtthPercentage", "WirelessPercentage", 
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
SELECT 
    gen_random_uuid(),
    b."Id",
    COALESCE(
        (SELECT "WiredPercentage" FROM "ConnectivityAssessments" ca 
         WHERE ca."LocationId" = b."Id" AND ca."LocationType" = 'Barangay' 
         ORDER BY ca."LastUpdated" DESC LIMIT 1), 
        0
    ),
    COALESCE(
        (SELECT "WirelessPercentage" FROM "ConnectivityAssessments" ca 
         WHERE ca."LocationId" = b."Id" AND ca."LocationType" = 'Barangay' 
         ORDER BY ca."LastUpdated" DESC LIMIT 1), 
        0
    ),
    NOW(),
    'production-migration',
    NOW(),
    'production-migration',
    FALSE
FROM "Barangays" b
WHERE EXISTS (
    SELECT 1 FROM "ConnectivityAssessments" ca 
    WHERE ca."LocationId" = b."Id" AND ca."LocationType" = 'Barangay'
)
AND NOT EXISTS (
    SELECT 1 FROM "BarangayCoverages" bc WHERE bc."BarangayId" = b."Id"
);
```

3. **Deployment Steps:**
   - Run migration to create BarangayCoverage records
   - Deploy new code
   - Monitor report generation
   - Verify barangay counts are as expected

4. **Rollback Plan:**
   - Keep previous version deployed in parallel environment
   - If issues arise, route traffic back to old version
   - Investigate and fix BarangayCoverage data
   - Redeploy when ready

---

## Monitoring & Validation

### Post-Deployment Checks

1. **Verify Coverage Data:**
```sql
-- Check BarangayCoverage record counts by region
SELECT 
    r."Name" as region_name,
    COUNT(DISTINCT bc."Id") as coverage_records,
    COUNT(DISTINCT b."Id") as total_barangays,
    ROUND(COUNT(DISTINCT bc."Id")::numeric / COUNT(DISTINCT b."Id") * 100, 2) as coverage_percentage
FROM "Regions" r
JOIN "Provinces" p ON p."RegionId" = r."Id"
JOIN "Municipalities" m ON m."ProvinceId" = p."Id"
JOIN "Barangays" b ON b."MunicipalityId" = m."Id"
LEFT JOIN "BarangayCoverages" bc ON bc."BarangayId" = b."Id" AND bc."IsDeleted" = FALSE
GROUP BY r."Id", r."Name"
ORDER BY r."Name";
```

2. **Compare Report Outputs:**
   - Generate same reports before and after
   - Compare barangay counts
   - Verify expected reduction matches BarangayCoverage scope

3. **Monitor Errors:**
   - Watch for "no barangays found" errors
   - Check if regions have zero BarangayCoverage records
   - Alert if report generation fails

---

## Support & Troubleshooting

### Common Issues

**Issue:** Report shows no barangays
**Cause:** No BarangayCoverage records for selected area
**Solution:** Create BarangayCoverage entries for desired barangays

**Issue:** Report shows fewer barangays than expected
**Cause:** Some barangays missing BarangayCoverage records
**Solution:** Query to find missing barangays and create coverage records

**Issue:** Performance degradation
**Cause:** Missing index on BarangayCoverages.BarangayId
**Solution:** Verify unique index exists (created by migration)

### Diagnostic Queries

```sql
-- Find barangays without coverage in a municipality
SELECT b."Id", b."Name", b."Code"
FROM "Barangays" b
WHERE b."MunicipalityId" = 'YOUR-MUNICIPALITY-ID'
  AND NOT EXISTS (
    SELECT 1 FROM "BarangayCoverages" bc 
    WHERE bc."BarangayId" = b."Id" AND bc."IsDeleted" = FALSE
  );

-- Find orphaned coverage records (barangay doesn't exist)
SELECT bc.*
FROM "BarangayCoverages" bc
WHERE NOT EXISTS (
    SELECT 1 FROM "Barangays" b WHERE b."Id" = bc."BarangayId"
);
```

---

## Future Enhancements

1. **Bulk Management API**
   - Endpoint to create BarangayCoverage for multiple barangays at once
   - CSV import functionality
   - Bulk update operations

2. **Administrative UI**
   - Visual interface for managing coverage scope
   - Map-based selection of barangays
   - Coverage statistics dashboard

3. **Reporting Enhancements**
   - Report filter to show "in scope" vs "out of scope" comparison
   - Coverage gaps visualization
   - Historical coverage tracking

---

**Document Version:** 1.0  
**Last Updated:** December 28, 2025  
**Status:** Implemented & Ready for Testing
