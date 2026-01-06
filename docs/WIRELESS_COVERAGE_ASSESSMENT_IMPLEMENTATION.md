# Wireless Coverage Assessment Implementation

## Overview
Implemented wireless coverage computation and caching in the ConnectivityAssessment entity, following the same pattern used for wired broadband reports. This allows the background job to pre-compute wireless coverage data for barangays, improving report generation performance.

## Changes Made

### 1. Entity Changes

#### ConnectivityAssessment.cs
Added two new fields to store wireless coverage data:
- `WirelessCoverageJson` (string?) - Stores serialized `BarangayWirelessSummaryDto`
- `WirelessCoverageLastUpdated` (DateTime?) - Timestamp of last wireless coverage computation

### 2. DTOs Created

#### BarangayWirelessCoverageDto.cs
Created DTOs for wireless coverage data structure:
- **BarangayWirelessCoverageDto**: Per-provider wireless coverage breakdown
  - `ProviderName`: Name of the service provider
  - `Has2G/3G/4G/5G`: Boolean flags for technology presence
  - `Count2G/3G/4G/5G`: Count of cell sites per technology
  - `MedianTechnology`: Most prevalent technology generation
  - Speedtest statistics: Min/Max/Average download/upload speeds, latency, jitter
  - `TotalSpeedtests`: Count of speedtest records

- **BarangayWirelessSummaryDto**: Aggregated summary wrapper
  - `BarangayPsgcCode`: Barangay identifier
  - `Providers`: List of all provider coverage data
  - `LastUpdated`: Timestamp of computation

### 3. Service Implementation

#### WirelessCoverageComputationService.cs
Created a new service to compute wireless coverage data:
- **Interface**: `IWirelessCoverageComputationService`
- **Implementation**: `WirelessCoverageComputationService`

**Key Features**:
- Optimized queries using `AsNoTracking()` and projection patterns
- Database-side spatial filtering with `Within()` for PostGIS
- Groups cellulars by provider
- Computes technology flags, counts, and median technology
- Aggregates speedtest statistics (min/max/average for speeds, latency, jitter)
- Returns JSON string of `BarangayWirelessSummaryDto`

**Method**:
```csharp
Task<string?> ComputeBarangayWirelessCoverageAsync(
    string barangayPsgcCode, 
    Geometry boundary, 
    CancellationToken cancellationToken = default)
```

**Entity Navigation Paths Used**:
- Cellular location: `c.CellSector.Cell.Location.Within(boundary)`
- Provider name: `c.ServiceProvider.Provider.Name`
- Generation: `c.Technology.Generation.Name`
- Speedtest properties: `s.DownloadSpeed`, `s.UploadSpeed`, `s.Latency`, `s.Jitter`

### 4. Background Job Integration

#### ProcessConnectivityAssessmentJob.cs
Updated the connectivity assessment job to compute wireless coverage:

**Changes**:
- Added `IWirelessCoverageComputationService` injection
- Added `using NTC.QoS.Infrastructure.Services`
- In `GenerateNewAssessment` method:
  - Calls wireless coverage service for barangays only
  - Sets `WirelessCoverageJson` and `WirelessCoverageLastUpdated` fields
  - Handles null boundary cases gracefully

**Code**:
```csharp
// Compute wireless coverage for barangays
if (locationType == LocationTypeEnum.Barangay && boundary != null)
{
    assessment.WirelessCoverageJson = await _wirelessCoverageService
        .ComputeBarangayWirelessCoverageAsync(location.PSGCCode, boundary, cancellationToken);
    assessment.WirelessCoverageLastUpdated = DateTime.UtcNow;
}
```

### 5. Dependency Injection

#### ReportingServiceExtensions.cs
Registered the wireless coverage service:
```csharp
services.AddScoped<IWirelessCoverageComputationService, WirelessCoverageComputationService>();
```

### 6. Database Migration

#### 20251225160215_AddWirelessCoverageToConnectivityAssessment.cs
Created migration to add wireless coverage fields to ConnectivityAssessments table:
- Added `WirelessCoverageJson` column (text, nullable)
- Added `WirelessCoverageLastUpdated` column (timestamp with time zone, nullable)
- Also includes ReportTemplates table (pending entity)

## Performance Optimizations

1. **Query Optimization**:
   - Uses `AsNoTracking()` for read-only queries
   - Projects only required fields using `Select()`
   - Database-side spatial filtering with PostGIS `Within()`
   - Materializes data with `ToListAsync()` before grouping

2. **Caching Strategy**:
   - Pre-computes wireless coverage during assessment job
   - Stores as JSON in ConnectivityAssessment entity
   - Report handlers can read from cached data instead of real-time computation
   - Tracks last update timestamp for cache invalidation

3. **Memory Efficiency**:
   - Streams data from database in projected form
   - In-memory grouping and aggregation on smaller dataset
   - JSON serialization only for final result

## Usage Pattern

### In Reports
Wireless broadband report handlers should:
1. Query ConnectivityAssessment for the barangay
2. Check if `WirelessCoverageJson` is populated and recent
3. Deserialize JSON to `BarangayWirelessSummaryDto`
4. Use the pre-computed data instead of querying Cellulars/Speedtests directly

Example:
```csharp
var assessment = await _context.ConnectivityAssessments
    .FirstOrDefaultAsync(a => a.LocationPSGCCode == barangayPsgcCode);

if (!string.IsNullOrEmpty(assessment?.WirelessCoverageJson))
{
    var wirelessData = JsonSerializer.Deserialize<BarangayWirelessSummaryDto>(
        assessment.WirelessCoverageJson);
    // Use wirelessData.Providers for report generation
}
```

## Testing Checklist

- [ ] Apply migration to local database
- [ ] Run connectivity assessment job for test barangays
- [ ] Verify WirelessCoverageJson is populated correctly
- [ ] Verify WirelessCoverageLastUpdated timestamp is set
- [ ] Test JSON deserialization works correctly
- [ ] Update wireless broadband report handlers to use cached data
- [ ] Performance testing with large datasets (millions of records)
- [ ] Verify spatial queries work correctly with PostGIS
- [ ] Test with barangays having no cellulars/speedtests (edge cases)

## Next Steps

1. **Apply Migration**: User will manually apply the migration to local database
2. **Test Assessment Job**: Run the job and verify wireless coverage computation
3. **Update Report Handlers**: Modify wireless broadband report handlers to read from cached data
4. **Performance Testing**: Test with production-scale datasets
5. **Documentation**: Update API documentation with new DTOs

## Technical Notes

### Entity Relationships Verified
- `Cellular.Technology.Generation` - Correct path to generation name
- `Cellular.CellSector.Cell.Location` - Correct path to cell location
- `Cellular.ServiceProvider.Provider.Name` - Correct path to provider name
- `Speedtest.DownloadSpeed/UploadSpeed` - Correct property names (not DownloadSpeedMbps)

### Design Decisions
1. **JSON Storage**: Chose JSON over separate entity for flexibility and consistency with wired pattern
2. **Barangay-Only**: Only computing for barangays (LocationTypeEnum.Barangay) since they have boundaries
3. **Nullable Fields**: Made fields nullable to support existing assessments without wireless data
4. **Scoped Service**: Registered as scoped to align with DbContext lifetime

## Files Modified

1. `NTC.QoS.Domain\Entities\ConnectivityAssessment.cs` - Added wireless fields
2. `NTC.QoS.Domain\Models\DTOs\WirelessCoverage\BarangayWirelessCoverageDto.cs` - New DTO file
3. `NTC.QoS.Infrastructure\Services\WirelessCoverageComputationService.cs` - New service file
4. `NTC.QoS.Infrastructure\BackgroundTasks\ProcessConnectivityAssessmentJob.cs` - Added wireless computation
5. `NTC.QoS.Infrastructure\Extensions\ReportingServiceExtensions.cs` - Service registration
6. `NTC.QoS.Infrastructure\Migrations\20251225160215_AddWirelessCoverageToConnectivityAssessment.cs` - New migration

## Migration Command

To apply the migration:
```bash
dotnet ef database update --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API
```

To rollback if needed:
```bash
dotnet ef migrations remove --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API
```
