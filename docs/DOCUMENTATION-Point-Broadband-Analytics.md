# Point-Based Broadband Info Analytics API

## Overview

The new `POST /analytics/point-broadband-info` endpoint extends the existing area broadband analytics functionality by allowing users to query available services (NAPs and cells) within a circular radius around a specific point location, instead of using a rectangular bounding box.

## Endpoint Details

### URL
```
POST /analytics/point-broadband-info
```

### Request Body
```json
{
  "latitude": 14.5995,
  "longitude": 120.9842,
  "radiusMeters": 5000
}
```

### Parameters
- **latitude** (double): Latitude coordinate of the center point (-5° to 25°)
- **longitude** (double): Longitude coordinate of the center point (110° to 135°)
- **radiusMeters** (double): Radius in meters from the center point (1 to 50,000 meters)

### Response
Returns the same `AreaBroadbandInfoDto` structure as the bounding box endpoint, but with additional circular area information:

```json
{
  "succeeded": true,
  "message": "Point broadband info retrieved successfully. Found 45 cells and 23 NAPs from 3 service providers within 5000m radius.",
  "data": {
    "summary": {
      "totalCells": 45,
      "totalNaps": 23,
      "totalAssets": 68,
      "coverageAreaKm2": 78.54,
      "serviceProviderCount": 3,
      "municipalityCount": 2,
      "assetDensityPerKm2": 0.866
    },
    "serviceProviders": [
      {
        "id": "guid",
        "name": "Smart Communications",
        "color": "#FF0000",
        "serviceType": "Wireless",
        "cellCount": 25,
        "napCount": 15,
        "totalAssets": 40
      }
    ],
    "municipalities": [
      {
        "id": "guid",
        "name": "Manila",
        "cellCount": 30,
        "napCount": 18,
        "totalAssets": 48,
        "assetPercentage": 70.59,
        "hasBoundaryData": true,
        "displayName": "Manila"
      }
    ],
    "envelopeInfo": {
      "areaSizeKm2": 78.54,
      "geometryType": "Circle",
      "centerPoint": {
        "latitude": 14.5995,
        "longitude": 120.9842
      },
      "radiusMeters": 5000,
      "boundingBox": {
        "minLatitude": 14.5545,
        "maxLatitude": 14.6445,
        "minLongitude": 120.9392,
        "maxLongitude": 121.0292
      }
    }
  }
}
```

## Key Features

### 1. **Circular Spatial Query**
- Uses NetTopologySuite buffer operations to create a circular polygon
- More intuitive for "find services near me" use cases
- Accurate distance calculations using Haversine formula

### 2. **Distance-Based Filtering**
- All services are filtered by actual distance from the center point
- More precise than bounding box queries for proximity searches
- Eliminates corner artifacts present in rectangular queries

### 3. **Performance Optimizations**
- Maximum radius limited to 50km for performance
- Efficient spatial indexing using PostGIS
- Optimized queries with proper projections

### 4. **Enhanced Response Data**
- Includes center point coordinates in response
- Shows actual radius used for the query
- Provides calculated circular area in km²
- Maintains compatibility with existing UI components

## Use Cases

### 1. **Mobile Applications**
```typescript
// "Find services near my location"
const nearbyServices = await findNearbyServices({
  latitude: userLocation.lat,
  longitude: userLocation.lng,
  radiusMeters: 10000 // 10km radius
});
```

### 2. **Location-Based Planning**
```typescript
// "What services are available at this site?"
const siteAnalysis = await analyzeSiteServices({
  latitude: proposedSite.lat,
  longitude: proposedSite.lng,
  radiusMeters: 5000 // 5km coverage area
});
```

### 3. **Service Coverage Analysis**
```typescript
// "Check coverage in rural areas"
const ruralCoverage = await checkRuralCoverage({
  latitude: 16.123456,
  longitude: 121.654321,
  radiusMeters: 25000 // 25km radius for rural analysis
});
```

## Validation Rules

### Geographic Constraints
- **Latitude**: -5° to 25° (Asia-Pacific region)
- **Longitude**: 110° to 135° (Asia-Pacific region)
- **Radius**: 1 to 50,000 meters (1m to 50km)

### Error Responses
```json
{
  "succeeded": false,
  "message": "Validation failed",
  "errors": {
    "PointRadius.Latitude": ["Latitude must be between -5° and 25°"],
    "PointRadius.RadiusMeters": ["Radius cannot exceed 50,000 meters (50km)"]
  }
}
```

## Technical Implementation

### Spatial Query Logic
1. **Point Creation**: Convert lat/lng to NetTopologySuite Point
2. **Buffer Creation**: Create circular polygon using buffer operation
3. **Spatial Filtering**: Apply spatial intersection with asset locations
4. **Distance Calculation**: Calculate actual distances using Haversine formula
5. **Municipal Analysis**: Identify municipalities within the circular area

### Database Optimization
- Uses PostGIS spatial indexes for efficient querying
- Leverages existing spatial infrastructure
- Maintains compatibility with current data model

## Comparison: Point vs Bounding Box Queries

| Feature | Bounding Box | Point + Radius |
|---------|--------------|----------------|
| **Query Shape** | Rectangular | Circular |
| **Use Case** | Area analysis | Proximity search |
| **Distance Precision** | Approximate | Exact |
| **Corner Artifacts** | Yes | No |
| **Mobile Friendly** | Moderate | High |
| **Intuitive** | Map-based | Location-based |

## Example Frontend Integration

```typescript
interface PointBroadbandRequest {
  latitude: number;
  longitude: number;
  radiusMeters: number;
}

const searchNearbyServices = async (request: PointBroadbandRequest) => {
  const response = await fetch('/analytics/point-broadband-info', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });
  
  return await response.json();
};

// Usage
const result = await searchNearbyServices({
  latitude: 14.5995,  // Manila
  longitude: 120.9842,
  radiusMeters: 5000  // 5km radius
});

console.log(`Found ${result.data.summary.totalAssets} assets within 5km`);
```

## Performance Considerations

- **Optimal Radius**: 1-10km for urban areas, up to 25km for rural
- **Response Time**: Typically <500ms for radii up to 10km
- **Data Volume**: Scales with asset density in the area
- **Caching**: Consider caching results for frequently queried locations

## Future Enhancements

1. **Multi-Point Queries**: Support multiple center points in one request
2. **Async Processing**: For very large radius queries
3. **Real-time Updates**: WebSocket notifications for service changes
4. **Historical Data**: Time-based analysis of service availability
5. **Heat Maps**: Visual representation of service density