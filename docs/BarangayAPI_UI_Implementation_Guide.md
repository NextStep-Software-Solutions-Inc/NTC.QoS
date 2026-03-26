# Barangay API - UI Implementation Guide

## Overview
This document provides guidance for UI developers on implementing the enhanced Barangay API endpoints, specifically the case-insensitive search functionality and boundary geometry display.

## What Changed

### 1. Case-Insensitive Search (GET `/barangays`)
The `name` query parameter now performs **case-insensitive** searches. Users can search for barangays using any letter case combination.

### 2. Boundaries Data (GET `/barangays/{id}`)
The individual barangay endpoint now returns boundary geometry data (`MultiPolygon`) that can be used to display barangay boundaries on maps.

---

## API Endpoints

### GET `/barangays` - List Barangays (Enhanced)

**Description**: Retrieve a paginated list of barangays with optional filters. The `name` parameter now supports case-insensitive search.

**Authentication**: Requires `ViewBarangays` permission

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageSize` | int | No | 10 | Number of items per page (1-100) |
| `pageNumber` | int | No | 1 | Page number (1-based) |
| `municipalityIds` | Guid[] | No | null | Filter by municipality IDs |
| `regionIds` | Guid[] | No | null | Filter by region IDs |
| `name` | string | No | null | **Case-insensitive** search by barangay name |
| `municipality` | string | No | null | **Case-insensitive** search by municipality name |
| `province` | string | No | null | **Case-insensitive** search by province name |
| `isWiredCovered` | bool | No | null | Filter by wired coverage status |
| `isWirelessCovered` | bool | No | null | Filter by wireless coverage status |

**Response**: `PaginatedResponse<BarangayDto>`

**Example Request**:
```http
GET /barangays?name=manila&pageSize=20&pageNumber=1
```

**Example Response**:
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "San Antonio (Malate)",
      "description": "Urban barangay in Manila",
      "municipality": {
        "id": "660e8400-e29b-41d4-a716-446655440000",
        "name": "Manila",
        "isCity": true,
        "province": {
          "id": "770e8400-e29b-41d4-a716-446655440000",
          "name": "Metro Manila",
          "region": {
            "id": "880e8400-e29b-41d4-a716-446655440000",
            "name": "National Capital Region"
          }
        }
      },
      "code": "137404017",
      "area": 0.245
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 897,
  "totalPages": 45
}
```

**UI Implementation Notes**:
- **Search Input**: Implement a debounced search input (300-500ms delay) for the `name` parameter
- **Case Handling**: No need to normalize user input - the API handles all cases automatically
- **User Feedback**: Show search results in real-time as user types
- **Empty State**: Display a "No barangays found" message when search returns zero results

**Example Search Behavior**:
```javascript
// All these searches will return the same results:
GET /barangays?name=Manila
GET /barangays?name=manila
GET /barangays?name=MANILA
GET /barangays?name=MaNiLa
```

---

### GET `/barangays/{id}` - Get Barangay Details (Enhanced)

**Description**: Retrieve detailed information for a specific barangay, including boundary geometry data.

**Authentication**: Requires `ViewBarangays` permission

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Barangay unique identifier |

**Response**: `ApiResponse<BarangayDto>`

**Example Request**:
```http
GET /barangays/550e8400-e29b-41d4-a716-446655440000
```

**Example Response**:
```json
{
  "succeeded": true,
  "message": "Barangay retrieved successfully.",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "San Antonio (Malate)",
    "description": "Urban barangay in Manila",
    "municipality": {
      "id": "660e8400-e29b-41d4-a716-446655440000",
      "name": "Manila",
      "isCity": true,
      "description": "Capital city of the Philippines",
      "province": {
        "id": "770e8400-e29b-41d4-a716-446655440000",
        "name": "Metro Manila",
        "description": "National Capital Region",
        "region": {
          "id": "880e8400-e29b-41d4-a716-446655440000",
          "name": "National Capital Region",
          "description": "NCR"
        }
      }
    },
    "code": "137404017",
    "area": 0.245,
    "boundaries": {
      "type": "MultiPolygon",
      "coordinates": [
        [
          [
            [120.9842, 14.5601],
            [120.9850, 14.5605],
            [120.9845, 14.5610],
            [120.9838, 14.5607],
            [120.9842, 14.5601]
          ]
        ]
      ]
    }
  }
}
```

---

## UI Implementation Guidelines

### 1. Search Functionality

#### Recommended Component Structure
```jsx
import { useState, useEffect } from 'react';
import { debounce } from 'lodash';

function BarangaySearch() {
  const [searchTerm, setSearchTerm] = useState('');
  const [results, setResults] = useState([]);
  const [loading, setLoading] = useState(false);

  // Debounced search function
  const debouncedSearch = debounce(async (term) => {
    if (!term || term.length < 2) {
      setResults([]);
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(
        `/barangays?name=${encodeURIComponent(term)}&pageSize=10`
      );
      const data = await response.json();
      setResults(data.data);
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setLoading(false);
    }
  }, 300);

  useEffect(() => {
    debouncedSearch(searchTerm);
    return () => debouncedSearch.cancel();
  }, [searchTerm]);

  return (
    <div>
      <input
        type="text"
        placeholder="Search barangays... (case-insensitive)"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
      />
      {loading && <div>Searching...</div>}
      {results.length > 0 && (
        <ul>
          {results.map((barangay) => (
            <li key={barangay.id}>{barangay.name}</li>
          ))}
        </ul>
      )}
      {!loading && searchTerm.length >= 2 && results.length === 0 && (
        <div>No barangays found for "{searchTerm}"</div>
      )}
    </div>
  );
}
```

#### Best Practices
- **Minimum Characters**: Start search after 2-3 characters to reduce unnecessary API calls
- **Debouncing**: Use 300-500ms debounce to avoid excessive requests while typing
- **Loading State**: Show a loading indicator during API calls
- **Error Handling**: Handle network errors gracefully and show user-friendly messages
- **Clear Button**: Provide a way to clear the search input
- **Highlight Matches**: Consider highlighting the matching text in results

### 2. Map Integration with Boundaries

#### Using Leaflet
```javascript
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

async function displayBarangayBoundary(barangayId, map) {
  try {
    const response = await fetch(`/barangays/${barangayId}`);
    const result = await response.json();
    
    if (result.succeeded && result.data.boundaries) {
      const boundaries = result.data.boundaries;
      
      // Convert GeoJSON to Leaflet layer
      const geoJsonLayer = L.geoJSON(boundaries, {
        style: {
          color: '#3388ff',
          weight: 2,
          opacity: 0.8,
          fillOpacity: 0.2
        }
      });
      
      // Add to map and fit bounds
      geoJsonLayer.addTo(map);
      map.fitBounds(geoJsonLayer.getBounds());
      
      // Add popup with barangay info
      geoJsonLayer.bindPopup(`
        <strong>${result.data.name}</strong><br>
        Municipality: ${result.data.municipality.name}<br>
        Area: ${result.data.area} km²
      `);
    }
  } catch (error) {
    console.error('Failed to load boundaries:', error);
  }
}

// Usage
const map = L.map('map').setView([14.5995, 120.9842], 13);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);

displayBarangayBoundary('550e8400-e29b-41d4-a716-446655440000', map);
```

#### Using Mapbox GL JS
```javascript
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';

async function displayBarangayBoundary(barangayId, map) {
  try {
    const response = await fetch(`/barangays/${barangayId}`);
    const result = await response.json();
    
    if (result.succeeded && result.data.boundaries) {
      const sourceId = `barangay-${barangayId}`;
      const layerId = `barangay-layer-${barangayId}`;
      
      // Find the first symbol/label layer so we can insert BEFORE it
      // This keeps map labels (cities, streets, etc.) visible above the fill overlay
      const firstSymbolId = map.getStyle().layers.find(l => l.type === 'symbol')?.id;
      
      // Add source
      map.addSource(sourceId, {
        type: 'geojson',
        data: result.data.boundaries
      });
      
      // Add fill layer – inserted BEFORE the first symbol layer to keep labels visible
      map.addLayer({
        id: layerId,
        type: 'fill',
        source: sourceId,
        paint: {
          'fill-color': '#3388ff',
          'fill-opacity': 0.2
        }
      }, firstSymbolId);   // ← beforeId: insert before labels
      
      // Add outline layer – also before labels
      map.addLayer({
        id: `${layerId}-outline`,
        type: 'line',
        source: sourceId,
        paint: {
          'line-color': '#3388ff',
          'line-width': 2
        }
      }, firstSymbolId);   // ← beforeId: insert before labels
      
      // Fit map to boundaries
      const bounds = new mapboxgl.LngLatBounds();
      result.data.boundaries.coordinates[0][0].forEach(coord => {
        bounds.extend(coord);
      });
      map.fitBounds(bounds, { padding: 50 });
    }
  } catch (error) {
    console.error('Failed to load boundaries:', error);
  }
}

// Usage
mapboxgl.accessToken = 'YOUR_MAPBOX_TOKEN';
const map = new mapboxgl.Map({
  container: 'map',
  style: 'mapbox://styles/mapbox/streets-v11',
  center: [120.9842, 14.5995],
  zoom: 13
});

map.on('load', () => {
  displayBarangayBoundary('550e8400-e29b-41d4-a716-446655440000', map);
});
```

#### Best Practices for Maps
- **Performance**: Cache boundary data to avoid repeated API calls
- **Styling**: Use consistent colors and styles across your application
- **Interactivity**: Add click/hover events to show barangay details
- **Multiple Boundaries**: When displaying multiple barangays, use different colors or layers
- **Loading State**: Show a loading spinner while fetching boundary data
- **Error Handling**: Handle cases where boundaries might be null or invalid
- **Label Visibility**: Always use `beforeId` (first symbol layer) when adding fill/coverage layers to keep map labels readable — see the note below

---

> **⚠ Frontend Note – Label Visibility Across All Map Styles**
>
> Any fill or line layer (including the Fiber Networks coverage layer) MUST be
> inserted **before** the map style's first symbol/label layer so that place-name
> labels (streets, cities, landmarks) remain legible across all supported map tile
> styles (Streets, Topography Dark, Basic, etc.).
>
> Find the first symbol layer and pass it as `beforeId` to `map.addLayer()`:
>
> ```javascript
> // Works with MapLibre GL JS / Mapbox GL JS for all supported tile styles
> const firstSymbolId = map.getStyle().layers.find(l => l.type === 'symbol')?.id;
>
> map.addLayer({
>   id: 'fiber-coverage-fill',
>   type: 'fill',
>   source: 'fiber-coverage',
>   paint: { 'fill-color': '#c026d3', 'fill-opacity': 0.35 }
> }, firstSymbolId);   // ← insert BEFORE the first label layer
> ```
>
> Without `beforeId` the layer is appended at the top of the rendering stack and
> will obscure place-name labels on every map style.

### 3. Boundary Data Format

The `boundaries` property follows the GeoJSON MultiPolygon format:

```typescript
interface BarangayDto {
  id: string;
  name: string;
  description?: string;
  municipality?: MunicipalityDto;
  code?: string;
  area?: number;
  boundaries?: MultiPolygon;  // NEW: GeoJSON MultiPolygon
}

interface MultiPolygon {
  type: "MultiPolygon";
  coordinates: number[][][][];  // Array of polygons, each with rings of [longitude, latitude] pairs
}
```

**Important Notes**:
- Coordinates are in **[longitude, latitude]** order (not latitude, longitude)
- The first polygon in a MultiPolygon is the outer boundary
- Subsequent polygons represent holes (if any)
- Each polygon is closed (first and last coordinates are the same)
- Boundaries may be `null` for some barangays if data is not available

### 4. Error Handling

```javascript
async function fetchBarangay(id) {
  try {
    const response = await fetch(`/barangays/${id}`);
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    const result = await response.json();
    
    if (!result.succeeded) {
      // Handle API-level errors
      console.error('API Error:', result.message, result.errors);
      return null;
    }
    
    return result.data;
  } catch (error) {
    console.error('Network error:', error);
    return null;
  }
}

// Usage
const barangay = await fetchBarangay(barangayId);
if (barangay) {
  if (barangay.boundaries) {
    // Display boundaries on map
  } else {
    // Show message: "Boundary data not available"
  }
}
```

---

## Migration Notes

### What UI Code Needs to Change?

#### ✅ No Changes Required
- Existing search implementations will continue to work
- The API remains backward compatible
- Boundary data is optional (won't break existing code)

#### 🔄 Recommended Changes
1. **Search Instructions**: Update UI text to inform users that search is case-insensitive
2. **Map Features**: Implement boundary visualization if displaying barangays on maps
3. **Detail Pages**: Add boundary visualization to barangay detail pages

#### Example UI Text Updates
```diff
- Search barangays (exact match)
+ Search barangays (case-insensitive)

- Enter barangay name exactly as it appears
+ Enter barangay name in any case
```

---

## Testing Checklist

### Search Functionality
- [ ] Search with lowercase input (e.g., "manila")
- [ ] Search with uppercase input (e.g., "MANILA")
- [ ] Search with mixed case (e.g., "MaNiLa")
- [ ] Search with partial names (e.g., "man")
- [ ] Search with special characters in names
- [ ] Verify pagination works with search results
- [ ] Test empty search results
- [ ] Test with very long search strings

### Boundary Display
- [ ] Verify boundaries render correctly on map
- [ ] Test with barangays that have complex boundaries
- [ ] Test with barangays that have no boundary data (should handle gracefully)
- [ ] Verify coordinates are in correct format [longitude, latitude]
- [ ] Test zoom/fit to boundaries functionality
- [ ] Test multiple boundaries on same map
- [ ] Test boundary styling and interactivity

### Performance
- [ ] Search response time is acceptable (<500ms)
- [ ] Debouncing prevents excessive API calls
- [ ] Boundary data loads efficiently
- [ ] Large polygon data doesn't freeze the UI
- [ ] Map rendering is smooth

---

## Support

For questions or issues related to these API changes, please contact:
- Backend Team: API implementation questions
- DevOps Team: Environment-specific issues
- Product Team: Feature requirements clarification

---

## Changelog

### Version 1.2 (March 2026)
- 🐛 **FIX**: Documented mandatory `beforeId` pattern to prevent coverage overlay layers (e.g. Fiber Networks) from obscuring map labels across all tile styles (Streets, Topography Dark, Basic)

### Version 1.1 (January 2026)
- ✨ **NEW**: Case-insensitive search for `name`, `municipality`, and `province` parameters
- ✨ **NEW**: `boundaries` property added to `BarangayDto` (MultiPolygon geometry)
- 🔧 Updated search implementation to use PostgreSQL `ILIKE` for better performance

### Version 1.0 (Previous)
- Initial barangay API implementation
- Case-sensitive search only
- No boundary data in responses
