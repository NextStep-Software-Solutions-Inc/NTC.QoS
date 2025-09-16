# Connectivity Analysis API - CMTS Broadband Mapping Implementation

This document outlines the implementation of internet connectivity analysis based on the CMTS (Commission on Information and Communications Technology) Broadband Mapping Rubric for the Philippines.

## Overview

The connectivity analysis system evaluates internet access across different administrative levels (Barangay, Municipality, Province, Region) using a standardized scoring methodology that classifies areas as **Served**, **Underserved**, or **Unserved**.

## Database Schema Additions

### New Entities

#### 1. ConnectivityAssessment
Stores comprehensive connectivity evaluations for geographic areas.

```csharp
public class ConnectivityAssessment : BaseEntity, IGeoLocatedEntity
{
    // Location Information
    public Guid LocationId { get; set; }                    // Administrative area ID
    public LocationTypeEnum LocationType { get; set; }       // Barangay/Municipality/Province/Region
    public Point Location { get; set; }                     // Geographic center point
    public MultiPolygon? Boundary { get; set; }             // Area boundaries
    public double AreaSizeKm2 { get; set; }                 // Area size in km²
    
    // Assessment Results
    public ConnectivityStatusEnum ConnectivityStatus { get; set; } // Served/Underserved/Unserved
    public int TotalScore { get; set; }                      // 0-15 total CMTS score
    
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
    
    // Speed Test Analytics
    public decimal? AverageDownloadSpeedMbps { get; set; }
    public decimal? MedianDownloadSpeedMbps { get; set; }
    public bool MeetsMinimumSpeedRequirement { get; set; }   // ?25 Mbps standard
    
    // Metadata
    public DateTime AssessmentDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string AssessedBy { get; set; }
}
```

#### 2. WiFiHotspot
Tracks WiFi access points for comprehensive coverage analysis.

```csharp
public class WiFiHotspot : BaseEntity, IGeoLocatedEntity
{
    public Guid ServiceProviderId { get; set; }
    public string Name { get; set; }                        // Hotspot SSID/Name
    public string Address { get; set; }                     // Physical location
    public Point Location { get; set; }                     // GPS coordinates
    public double CoverageRadiusMeters { get; set; }        // Coverage area
    public bool IsPublic { get; set; }                      // Public accessibility
    public bool IsActive { get; set; }                      // Operational status
    public decimal? MaxBandwidthMbps { get; set; }          // Bandwidth capacity
    public string? FrequencyBand { get; set; }              // 2.4GHz, 5GHz, etc.
}
```

### New Enums

```csharp
public enum ConnectivityStatusEnum
{
    Served = 3,         // 11-15 points
    Underserved = 2,    // 6-10 points  
    Unserved = 1        // ?5 points
}

public enum LocationTypeEnum
{
    Barangay = 1,
    Municipality = 2,
    Provincial = 3,
    Regional = 4
}
```

## API Endpoints

### 1. Connectivity Analysis
**`POST /analytics/connectivity-analysis`**

Analyzes connectivity status for specified locations using existing assessment data.

```json
{
  "locationIds": ["guid1", "guid2", "guid3"],
  "locationType": "Municipality",
  "filterByStatus": "Underserved",
  "includeSpeedTestData": true,
  "includeInfrastructureData": true
}
```

**Response:**
```json
{
  "succeeded": true,
  "message": "Connectivity analysis completed for 3 Municipalities. Served: 1, Underserved: 1, Unserved: 1",
  "data": {
    "locationType": "Municipality",
    "locations": [
      {
        "locationId": "guid1",
        "locationName": "Makati City",
        "connectivityStatus": "Served",
        "totalScore": 13,
        "cmtsScores": {
          "fiberCoverage": 3,
          "internetAccess": 3,
          "technologyType": 3,
          "qualityOfService": 2,
          "coverageAvailability": 2
        },
        "infrastructure": {
          "cellTowerCount": 45,
          "fiberNAPCount": 23,
          "wiFiHotspotCount": 12,
          "serviceProviderCount": 4,
          "dominantServiceProvider": "Globe Telecom"
        },
        "speedStats": {
          "averageDownloadSpeedMbps": 85.4,
          "medianDownloadSpeedMbps": 78.2,
          "averageLatencyMs": 25.3,
          "speedTestCount": 1247,
          "meetsMinimumSpeedRequirement": true
        },
        "coverage": {
          "mobileCoveragePercentage": 98.5,
          "fixedBroadbandCoveragePercentage": 85.2,
          "fiberCoveragePercentage": 67.8,
          "hasReliableConnectivity": true
        },
        "areaSizeKm2": 27.36,
        "population": 629301,
        "assessmentDate": "2024-01-15T10:30:00Z"
      }
    ],
    "summary": {
      "totalLocations": 3,
      "servedCount": 1,
      "underservedCount": 1,
      "unservedCount": 1,
      "servedPercentage": 33.33,
      "underservedPercentage": 33.33,
      "unservedPercentage": 33.33,
      "totalInfrastructure": {
        "cellTowerCount": 78,
        "fiberNAPCount": 34,
        "wiFiHotspotCount": 15,
        "serviceProviderCount": 5
      },
      "totalAreaKm2": 156.42,
      "totalPopulation": 1205634,
      "averageScore": 8.33
    }
  }
}
```

### 2. Generate Connectivity Assessment
**`POST /analytics/generate-connectivity-assessment`**

Calculates new connectivity assessments based on current infrastructure and speed test data.

```json
{
  "locationIds": ["guid1", "guid2"],
  "locationType": "Barangay", 
  "forceRecalculate": false
}
```

This endpoint:
1. Analyzes infrastructure within location boundaries
2. Calculates speed test statistics  
3. Applies CMTS scoring rubric
4. Determines connectivity status
5. Stores results in the database

### 3. CMTS Rubric Information
**`GET /analytics/connectivity-rubric`**

Returns the complete CMTS scoring methodology and criteria definitions.

## CMTS Scoring Methodology

### Criteria and Point Values

| Criterion | Served (3 pts) | Underserved (2 pts) | Unserved (1 pt) |
|-----------|---------------|-------------------|----------------|
| **Fiber Coverage** | FTTH NAP facilities for all service providers present | Few major Telco NAPs from local/different VAS providers | No installed/present FTTH facilities |
| **Internet Access** | Stable internet ?25 Mbps meeting broadband standards | Available but slow/unreliable <25 Mbps | No/minimal internet access |
| **Technology Type** | Fiber optic, LTE/5G, high-speed DSL active | Legacy technologies (2G/3G, dial-up, satellite) | No broadband technology, radio signals only |
| **Quality of Service** | High quality, low latency, suitable for streaming/remote work | Fair quality with congestion/consistency issues | Poor quality, frequent disconnections |
| **Coverage Availability** | 100% household/institution coverage | 40-99% coverage, clustered in specific areas | <40% coverage, sparse infrastructure |

### Final Classification

- **Served**: 11-15 total points
- **Underserved**: 6-10 total points  
- **Unserved**: ?5 total points

## Implementation Guide

### 1. Database Migration

Add the new entities to your `ApplicationDbContext`:

```csharp
public DbSet<ConnectivityAssessment> ConnectivityAssessments { get; set; }
public DbSet<WiFiHotspot> WiFiHotspots { get; set; }
```

### 2. Register Repositories

In your DI container:

```csharp
services.AddScoped<IConnectivityAssessmentRepository, ConnectivityAssessmentRepository>();
services.AddScoped<IWiFiHotspotRepository, WiFiHotspotRepository>();
```

### 3. Sample Usage

#### Generate Assessment for All Barangays in Metro Manila
```csharp
// Get all barangay IDs in Metro Manila
var barangayIds = await barangayRepository.GetBarangayIdsByRegion("Metro Manila");

// Generate assessments
var request = new GenerateConnectivityAssessmentRequest
{
    LocationIds = barangayIds,
    LocationType = LocationTypeEnum.Barangay,
    ForceRecalculate = true
};

var result = await mediator.Send(request);
```

#### Query Underserved Municipalities
```csharp
var analysisRequest = new GetConnectivityAnalysisRequest
{
    LocationIds = municipalityIds,
    LocationType = LocationTypeEnum.Municipality,
    FilterByStatus = ConnectivityStatusEnum.Underserved
};

var analysis = await mediator.Send(analysisRequest);
```

## Data Requirements

To fully utilize this system, ensure you have:

### Required Data
- ? Geographic boundaries (Barangay, Municipality, Province, Region) - **Already available**
- ? Cell tower locations and service providers - **Already available**
- ? Fiber NAP locations - **Already available** 
- ? Speed test data with locations - **Already available**

### Additional Data Needed
- ?? **WiFi Hotspot data** - Public and private WiFi access points
- ?? **Population data** - For coverage percentage calculations
- ?? **Household counts** - For household-level connectivity metrics
- ?? **Service provider service areas** - For coverage availability scoring

### Optional Enhancements
- Signal strength measurements (RSSI/SINR data)
- Technology generation mapping (2G/3G/4G/5G coverage areas)
- Affordability metrics (pricing data)
- Digital literacy indicators

## Frontend Integration

### Dashboard Components

#### 1. Connectivity Status Map
```typescript
interface ConnectivityMapProps {
  locationType: LocationTypeEnum;
  data: LocationConnectivityDto[];
}

const getStatusColor = (status: ConnectivityStatusEnum) => {
  switch(status) {
    case ConnectivityStatusEnum.Served: return '#28a745';      // Green
    case ConnectivityStatusEnum.Underserved: return '#ffc107'; // Yellow  
    case ConnectivityStatusEnum.Unserved: return '#dc3545';    // Red
  }
};
```

#### 2. CMTS Score Breakdown
```typescript
const CMTSScoreCard = ({ scores }: { scores: CMTSScoresDto }) => (
  <div className="score-breakdown">
    <div className="score-item">
      <span>Fiber Coverage</span>
      <div className="score-bar">
        <div style={{width: `${(scores.fiberCoverage / 3) * 100}%`}} />
      </div>
      <span>{scores.fiberCoverage}/3</span>
    </div>
    {/* Repeat for other criteria */}
  </div>
);
```

#### 3. Service Gap Analysis
```typescript
const ServiceGapAnalysis = ({ summary }: { summary: ConnectivitySummaryDto }) => {
  const gapPercentage = summary.underservedPercentage + summary.unservedPercentage;
  
  return (
    <div className="gap-analysis">
      <h3>Service Gaps Identified</h3>
      <p>{gapPercentage}% of areas need connectivity improvements</p>
      <ul>
        <li>{summary.unservedCount} areas with no broadband infrastructure</li>
        <li>{summary.underservedCount} areas with inadequate connectivity</li>
      </ul>
    </div>
  );
};
```

## Performance Considerations

1. **Spatial Indexing**: Ensure geometry columns have spatial indexes for efficient boundary queries
2. **Assessment Caching**: Cache assessment results for 30 days unless forced recalculation
3. **Batch Processing**: Process large location sets in batches to avoid timeouts
4. **Background Jobs**: Consider running assessment generation as background tasks for large datasets

## Monitoring and Alerts

Set up monitoring for:
- Assessment generation success rates
- Data freshness (speed tests, infrastructure updates)
- API response times for large location sets
- Assessment accuracy validation against known connectivity status

This implementation provides a comprehensive foundation for connectivity analysis while following the official CMTS guidelines for Philippine broadband mapping.