# NTC QoS Analytics API Documentation

## Overview

The Analytics API provides comprehensive broadband infrastructure analysis and connectivity assessment capabilities based on CMTS (Commission on Information and Communications Technology) Broadband Mapping Rubric for the Philippines.

## Key Features

1. **Area-based broadband analysis** - Query infrastructure within geographic boundaries
2. **Point-radius broadband analysis** - Query infrastructure within circular areas
3. **CMTS connectivity assessment** - Standardized connectivity scoring (Served/Underserved/Unserved)
4. **Signal strength utilities** - Comprehensive RSSI analysis and classification

## API Endpoints

### 1. Area Broadband Info
**`POST /analytics/area-broadband-info`**
```json
{
  "minLatitude": -5.0,
  "maxLatitude": 25.0,
  "minLongitude": 110.0,
  "maxLongitude": 135.0
}
```

### 2. Point Broadband Info  
**`POST /analytics/point-broadband-info`**
```json
{
  "latitude": 14.5995,
  "longitude": 120.9842,
  "radiusMeters": 10000
}
```

### 3. Connectivity Analysis
**`POST /analytics/connectivity-analysis`**

Uses separate request validation with FluentValidation.

```json
{
  "locationIds": ["guid1", "guid2"],
  "locationType": "Municipality",
  "filterByStatus": "Underserved",
  "includeSpeedTestData": true,
  "includeInfrastructureData": true
}
```

**Request Validation Rules:**
- LocationIds: Required, 1-100 locations, valid GUIDs only
- LocationType: Must be valid enum (Barangay, Municipality, Provincial, Regional)
- FilterByStatus: Optional, must be valid ConnectivityStatusEnum when provided
- At least one of IncludeSpeedTestData or IncludeInfrastructureData must be true

### 4. Generate Connectivity Assessment
**`POST /analytics/generate-connectivity-assessment`**

Uses separate request validation with FluentValidation.

```json
{
  "locationIds": ["guid1", "guid2"],
  "locationType": "Barangay",
  "forceRecalculate": false
}
```

**Request Validation Rules:**
- LocationIds: Required, 1-50 locations, valid GUIDs only  
- LocationType: Must be valid enum
- ForceRecalculate: Limited to 20 locations when true
- Regional assessments limited to 5 locations at once

### 5. CMTS Rubric & Geographic Constraints
- **`GET /analytics/connectivity-rubric`** - CMTS scoring methodology
- **`GET /analytics/geographic-constraints`** - Supported coordinate bounds

## Request Architecture

### Separated Request Classes
Each endpoint now uses dedicated request classes with comprehensive validation:

#### GetConnectivityAnalysisRequest
- **Location**: `NTC.QoS.Application/Features/Analytics/Requests/GetConnectivityAnalysisRequest.cs`
- **Validator**: `GetConnectivityAnalysisRequestValidator`
- **Max Locations**: 100 (performance optimized)

#### GenerateConnectivityAssessmentRequest  
- **Location**: `NTC.QoS.Application/Features/Analytics/Requests/GenerateConnectivityAssessmentRequest.cs`
- **Validator**: `GenerateConnectivityAssessmentRequestValidator`  
- **Max Locations**: 50 (computational complexity)

### FluentValidation Integration
- **Auto-validation** enabled in Application layer
- **Detailed error messages** for invalid requests
- **Business rule validation** (e.g., ForceRecalculate limits)
- **Performance constraints** built into validation rules

## Database Entities

### ConnectivityAssessment
```csharp
public class ConnectivityAssessment : BaseEntity, IGeoLocatedEntity
{
    // Location & Geography
    public Guid LocationId { get; set; }
    public LocationTypeEnum LocationType { get; set; }
    public Point Location { get; set; }
    public MultiPolygon? Boundary { get; set; }
    public double AreaSizeKm2 { get; set; }
    
    // CMTS Assessment Results
    public ConnectivityStatusEnum ConnectivityStatus { get; set; } // Served/Underserved/Unserved
    public int TotalScore { get; set; }                          // 0-15 total score
    
    // CMTS Criteria Scores (0-3 each)
    public int FiberCoverageScore { get; set; }
    public int InternetAccessScore { get; set; }
    public int TechnologyTypeScore { get; set; }
    public int QualityOfServiceScore { get; set; }
    public int CoverageAvailabilityScore { get; set; }
    
    // Infrastructure Metrics
    public int CellTowerCount { get; set; }
    public int FiberNAPCount { get; set; }
    public int WifiHotspotCount { get; set; }
    public int ServiceProviderCount { get; set; }
    
    // Speed Analytics
    public decimal? AverageDownloadSpeedMbps { get; set; }
    public decimal? MedianDownloadSpeedMbps { get; set; }
    public bool MeetsMinimumSpeedRequirement { get; set; }
    
    public DateTime AssessmentDate { get; set; }
    public string AssessedBy { get; set; }
}
```

### WiFiHotspot
```csharp
public class WiFiHotspot : BaseEntity, IGeoLocatedEntity
{
    public Guid ServiceProviderId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public Point Location { get; set; }
    public double CoverageRadiusMeters { get; set; }
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public decimal? MaxBandwidthMbps { get; set; }
    public string? FrequencyBand { get; set; }
}
```

## CMTS Scoring Methodology

### Connectivity Status Classification
- **Served** (11-15 points): Fully developed, meets broadband standards
- **Underserved** (6-10 points): Basic infrastructure but lacks quality/consistency  
- **Unserved** (≤5 points): Virtually no broadband access

### Scoring Criteria (0-3 points each)

| Criterion | Served (3pts) | Underserved (2pts) | Unserved (1pt) |
|-----------|---------------|-------------------|----------------|
| **Fiber Coverage** | FTTH NAP facilities for all providers | Few major Telco/local VAS NAPs | No FTTH facilities |
| **Internet Access** | Stable ?25 Mbps broadband | Available but <25 Mbps | No/minimal access |
| **Technology Type** | Fiber/LTE/5G active | Legacy 2G/3G/satellite | Radio signals only |
| **Quality of Service** | High quality, low latency | Fair with congestion issues | Poor, frequent disconnections |
| **Coverage Availability** | 100% household coverage | 40-99% coverage | <40% coverage |

## Signal Strength Utilities

### Available Classes
- **`SignalStrengthHelper`** - Static helper methods (`NTC.QoS.Domain.Helper`)
- **`SignalStrengthExtensions`** - Extension methods (`NTC.QoS.Domain.Extensions`)

### Quick Usage Examples

```csharp
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Extensions;

// Classify RSSI values
decimal rssi = 75;
SignalStrengthEnum strength = SignalStrengthHelper.ClassifyRSSI(rssi);
// or using extensions
SignalStrengthEnum strength2 = rssi.ToSignalStrength();

// Get descriptions and ranges  
string description = strength.GetDescription(); // "Average"
string range = strength.GetRSSIRange();        // "70-90 dBm"
bool meetsReq = strength.MeetsMinimumRequirement(); // true

// Parse quality strings
SignalStrengthEnum parsed = "good".ToSignalStrength();

// Calculate averages from collections
var rssiList = new List<decimal> { 60, 70, 80, 90 };
double avgStrength = rssiList.CalculateAverageSignalStrength();
```

### Signal Strength Classifications
- **Good**: 40–70 dBm
- **Average**: 70–80 dBm
- **Weak**: 80–90 dBm
- **Very Weak**: 90–100 dBm
- **No Broadband Coverage**: >100 dBm (no reliable signal detected)

## Geographic Constraints

### Supported Region (Asia-Pacific)
- **Latitude**: -5° to 25°
- **Longitude**: 110° to 135°
- **Maximum area**: 10° × 15° (≈1,100km × 1,300–1,600km)
- **Maximum radius**: 50,000 meters (50km)

## Implementation Setup

### 1. Database Registration
```csharp
// ApplicationDbContext
public DbSet<ConnectivityAssessment> ConnectivityAssessments { get; set; }
public DbSet<WiFiHotspot> WiFiHotspots { get; set; }
```

### 2. DI Registration  
```csharp
// Infrastructure layer
services.AddScoped<IConnectivityAssessmentRepository, ConnectivityAssessmentRepository>();
services.AddScoped<IWiFiHotspotRepository, WiFiHotspotRepository>();
services.AddScoped<IConnectivityCacheService, ConnectivityCacheService>();

// Application layer - FluentValidation auto-configured
services.AddFluentValidationAutoValidation();
services.Scan(scan => scan
    .FromAssemblies(Assembly.GetExecutingAssembly())
    .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

### 3. Request Handling Architecture
```csharp
// Controller maps DTO to MediatR request
[HttpPost("connectivity-analysis")]
public async Task<ActionResult<ApiResponse<ConnectivityAnalysisResponseDto>>> GetConnectivityAnalysis(
    [FromBody, Required] ConnectivityAnalysisRequestDto request)
{
    var result = await mediator.Send(new GetConnectivityAnalysisRequest
    {
        LocationIds = request.LocationIds,
        LocationType = request.LocationType,
        FilterByStatus = request.FilterByStatus,
        IncludeSpeedTestData = request.IncludeSpeedTestData,
        IncludeInfrastructureData = request.IncludeInfrastructureData
    });
    return result.Succeeded ? Ok(result) : BadRequest(result);
}

// Direct MediatR request for generate endpoint
[HttpPost("generate-connectivity-assessment")]
public async Task<ActionResult<ApiResponse<List<LocationConnectivityDto>>>> GenerateConnectivityAssessment(
    [FromBody, Required] GenerateConnectivityAssessmentRequest request)
{ 
    var result = await mediator.Send(request);
    return result.Succeeded ? Ok(result) : BadRequest(result);
}
```

## Validation Benefits

### Request Validation Features
- **Performance limits**: Max locations per request type
- **Business rules**: ForceRecalculate computational limits
- **Data quality**: GUID validation, enum validation
- **User guidance**: Detailed error messages
- **Type safety**: Strongly typed request classes

### Error Response Example
```json
{
  "succeeded": false,
  "message": "Validation failed",
  "errors": {
    "LocationIds": ["Cannot process more than 100 locations at once for performance reasons."],
    "LocationType": ["LocationType must be a valid enum value (Barangay, Municipality, Provincial, or Regional)."]
  }
}
```

## Performance Considerations

- **Separated request validation** for appropriate scaling limits
- **Assessment caching** (30-day default unless force recalculated)  
- **Batch processing** with location limits
- **Background job processing** for assessment generation
- **Spatial indexing** on geometry columns
- **FluentValidation** with minimal overhead

## Data Requirements

### Already Available ?
- Geographic boundaries (Barangay/Municipality/Province/Region)
- Cell tower locations and service providers
- Fiber NAP locations
- Speed test data with locations

### Additional Data Needed ??
- WiFi hotspot data (public/private access points)
- Population data for coverage calculations
- Household counts for connectivity metrics
- Service provider service areas

This consolidated documentation covers all analytics functionality with emphasis on the new separated request architecture and comprehensive validation system.