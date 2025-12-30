# Barangay Coverage API Documentation

## Overview

This document provides detailed information about the Barangay Coverage API endpoints for front-end integration. The API manages broadband coverage data (wired FTTH and wireless) for barangays across the Philippines.

## Base URL

All endpoints are under the base route:
```
/barangay-coverages
```

## Authentication

All endpoints require JWT Bearer Token authentication.
```
Authorization: Bearer {your-jwt-token}
```

## Endpoints

### 1. Create Barangay Coverage

Create coverage data for a specific barangay.

**Endpoint:** `POST /barangay-coverages`

**Permission Required:** `CreateBarangayCoverage`

**Request Body:**
```json
{
  "barangayId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "wiredFtthPercentage": 75.5,
  "wirelessPercentage": 90.0
}
```

**Field Validations:**
- `barangayId` (required): Must be a valid UUID of an existing barangay
- `wiredFtthPercentage` (required): Must be between 0 and 100
- `wirelessPercentage` (required): Must be between 0 and 100

**Success Response:**
```json
{
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "succeeded": true,
  "message": "Barangay coverage created successfully."
}
```
- Status Code: `200 OK`
- Returns the GUID of the newly created coverage record

**Error Responses:**
```json
{
  "data": null,
  "succeeded": false,
  "message": "Barangay not found."
}
```
- Status Code: `400 Bad Request`

```json
{
  "data": null,
  "succeeded": false,
  "message": "Coverage data already exists for this barangay. Use UPDATE instead."
}
```
- Status Code: `400 Bad Request`

---

### 2. Update Barangay Coverage

Update coverage data for an existing coverage record.

**Endpoint:** `PUT /barangay-coverages/{id}`

**Permission Required:** `UpdateBarangayCoverage`

**URL Parameters:**
- `id` (UUID): The ID of the coverage record to update

**Request Body:**
```json
{
  "wiredFtthPercentage": 80.0,
  "wirelessPercentage": 95.0
}
```

**Field Validations:**
- `wiredFtthPercentage` (required): Must be between 0 and 100
- `wirelessPercentage` (required): Must be between 0 and 100

**Success Response:**
- Status Code: `204 No Content`
- Empty body

**Error Responses:**
```json
{
  "message": "Coverage data with ID {id} not found."
}
```
- Status Code: `404 Not Found`

---

### 3. Get All Barangay Coverages (with filters)

Retrieve all coverage records with optional filtering by region, province, municipality, or barangay IDs.

**Endpoint:** `GET /barangay-coverages`

**Permission Required:** `ViewBarangayCoverage`

**Query Parameters (all optional):**
- `regionIds` (array of UUIDs): Filter by region IDs
- `provinceIds` (array of UUIDs): Filter by province IDs
- `municipalityIds` (array of UUIDs): Filter by municipality IDs
- `barangayIds` (array of UUIDs): Filter by barangay IDs

**Example Requests:**
```
GET /barangay-coverages
GET /barangay-coverages?regionIds=3fa85f64-5717-4562-b3fc-2c963f66afa6
GET /barangay-coverages?regionIds=3fa85f64-5717-4562-b3fc-2c963f66afa6&regionIds=4fa85f64-5717-4562-b3fc-2c963f66afa7
GET /barangay-coverages?provinceIds=3fa85f64-5717-4562-b3fc-2c963f66afa6&municipalityIds=4fa85f64-5717-4562-b3fc-2c963f66afa7
```

**Success Response:**
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "barangayId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "wiredFtthPercentage": 75.5,
      "wirelessPercentage": 90.0,
      "combinedPercentage": 100.0,
      "isWiredCovered": false,
      "isWirelessCovered": false,
      "updatedAt": "2024-12-29T10:30:00Z",
      "updatedBy": "admin@ntc.gov.ph"
    }
  ],
  "succeeded": true,
  "message": "Retrieved 1 coverage records."
}
```
- Status Code: `200 OK`

**Response Fields:**
- `id`: Unique identifier of the coverage record
- `barangayId`: ID of the barangay (no relationship data included for performance)
- `wiredFtthPercentage`: Percentage of wired FTTH coverage (0-100)
- `wirelessPercentage`: Percentage of wireless coverage (0-100)
- `combinedPercentage`: Computed as min(wiredFtthPercentage + wirelessPercentage, 100)
- `isWiredCovered`: True if wiredFtthPercentage equals 100
- `isWirelessCovered`: True if wirelessPercentage equals 100
- `updatedAt`: Timestamp of last update
- `updatedBy`: User who last updated the record

**Note:** This endpoint returns a lightweight response without relationship data (barangay name, municipality, etc.) for better performance. Use the single record endpoint if you need relationship data.

---

### 4. Get Single Barangay Coverage

Get detailed coverage data for a specific coverage record, including relationships.

**Endpoint:** `GET /barangay-coverages/{id}`

**Permission Required:** `ViewBarangayCoverage`

**URL Parameters:**
- `id` (UUID): The ID of the coverage record

**Success Response:**
```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "barangayId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "barangayName": "San Jose",
    "barangayCode": "137404001",
    "wiredFtthPercentage": 75.5,
    "wirelessPercentage": 90.0,
    "combinedPercentage": 100.0,
    "isWiredCovered": false,
    "isWirelessCovered": false,
    "updatedAt": "2024-12-29T10:30:00Z",
    "updatedBy": "admin@ntc.gov.ph"
  },
  "succeeded": true,
  "message": "Coverage data retrieved successfully."
}
```
- Status Code: `200 OK`

**Response Fields:**
Same as the list endpoint, plus:
- `barangayName`: Name of the barangay
- `barangayCode`: Official PSGC code of the barangay

**Note:** This endpoint includes relationship data (barangay name and code) but excludes geometry boundaries for better performance.

**Error Responses:**
```json
{
  "data": null,
  "succeeded": false,
  "message": "Coverage data not found."
}
```
- Status Code: `400 Bad Request`

---

### 5. Delete Barangay Coverage

Delete coverage data for a specific coverage record.

**Endpoint:** `DELETE /barangay-coverages/{id}`

**Permission Required:** `DeleteBarangayCoverage`

**URL Parameters:**
- `id` (UUID): The ID of the coverage record to delete

**Success Response:**
- Status Code: `204 No Content`
- Empty body

**Error Responses:**
```json
{
  "message": "Coverage data with ID {id} not found."
}
```
- Status Code: `404 Not Found`

---

## Computed Fields

The API automatically computes the following fields for all coverage records:

### combinedPercentage
```
min(wiredFtthPercentage + wirelessPercentage, 100)
```
Represents the total coverage, capped at 100% to avoid values exceeding full coverage.

### isWiredCovered
```
wiredFtthPercentage === 100
```
Boolean flag indicating complete wired FTTH coverage.

### isWirelessCovered
```
wirelessPercentage === 100
```
Boolean flag indicating complete wireless coverage.

---

## Common Error Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 204 | Success (No Content) |
| 400 | Bad Request - Validation error or business logic error |
| 401 | Unauthorized - Missing or invalid authentication token |
| 403 | Forbidden - User lacks required permission |
| 404 | Not Found - Resource does not exist |
| 500 | Internal Server Error |

---

## Usage Examples

### JavaScript/TypeScript (Fetch API)

#### Create Coverage
```typescript
async function createBarangayCoverage(barangayId: string, wiredPercentage: number, wirelessPercentage: number) {
  const response = await fetch('/barangay-coverages', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      barangayId,
      wiredFtthPercentage: wiredPercentage,
      wirelessPercentage: wirelessPercentage
    })
  });
  
  const result = await response.json();
  if (result.succeeded) {
    console.log('Coverage created with ID:', result.data);
  } else {
    console.error('Error:', result.message);
  }
}
```

#### Update Coverage
```typescript
async function updateBarangayCoverage(id: string, wiredPercentage: number, wirelessPercentage: number) {
  const response = await fetch(`/barangay-coverages/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      wiredFtthPercentage: wiredPercentage,
      wirelessPercentage: wirelessPercentage
    })
  });
  
  if (response.status === 204) {
    console.log('Coverage updated successfully');
  } else {
    const error = await response.json();
    console.error('Error:', error.message);
  }
}
```

#### Get All Coverages with Filters
```typescript
async function getBarangayCoverages(regionIds?: string[], provinceIds?: string[]) {
  const params = new URLSearchParams();
  
  regionIds?.forEach(id => params.append('regionIds', id));
  provinceIds?.forEach(id => params.append('provinceIds', id));
  
  const response = await fetch(`/barangay-coverages?${params.toString()}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const result = await response.json();
  if (result.succeeded) {
    console.log('Found coverages:', result.data);
  }
}
```

#### Get Single Coverage
```typescript
async function getBarangayCoverageById(id: string) {
  const response = await fetch(`/barangay-coverages/${id}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const result = await response.json();
  if (result.succeeded) {
    console.log('Coverage:', result.data);
  }
}
```

#### Delete Coverage
```typescript
async function deleteBarangayCoverage(id: string) {
  const response = await fetch(`/barangay-coverages/${id}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (response.status === 204) {
    console.log('Coverage deleted successfully');
  } else {
    const error = await response.json();
    console.error('Error:', error.message);
  }
}
```

---

## Angular/RxJS Example

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

interface CreateCoverageDto {
  barangayId: string;
  wiredFtthPercentage: number;
  wirelessPercentage: number;
}

interface UpdateCoverageDto {
  wiredFtthPercentage: number;
  wirelessPercentage: number;
}

interface BarangayCoverageLight {
  id: string;
  barangayId: string;
  wiredFtthPercentage: number;
  wirelessPercentage: number;
  combinedPercentage: number;
  isWiredCovered: boolean;
  isWirelessCovered: boolean;
  updatedAt: string;
  updatedBy: string;
}

interface BarangayCoverage extends BarangayCoverageLight {
  barangayName: string;
  barangayCode: string;
}

interface ApiResponse<T> {
  data: T;
  succeeded: boolean;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class BarangayCoverageService {
  private baseUrl = '/barangay-coverages';

  constructor(private http: HttpClient) {}

  create(dto: CreateCoverageDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateCoverageDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  getAll(
    regionIds?: string[],
    provinceIds?: string[],
    municipalityIds?: string[],
    barangayIds?: string[]
  ): Observable<ApiResponse<BarangayCoverageLight[]>> {
    let params = new HttpParams();
    
    regionIds?.forEach(id => params = params.append('regionIds', id));
    provinceIds?.forEach(id => params = params.append('provinceIds', id));
    municipalityIds?.forEach(id => params = params.append('municipalityIds', id));
    barangayIds?.forEach(id => params = params.append('barangayIds', id));
    
    return this.http.get<ApiResponse<BarangayCoverageLight[]>>(
      this.baseUrl, 
      { params }
    );
  }

  getById(id: string): Observable<ApiResponse<BarangayCoverage>> {
    return this.http.get<ApiResponse<BarangayCoverage>>(`${this.baseUrl}/${id}`);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
```

---

## React Query Example

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

// Query to get all coverages
export function useBarangayCoverages(filters?: {
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
}) {
  return useQuery({
    queryKey: ['barangay-coverages', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      
      filters?.regionIds?.forEach(id => params.append('regionIds', id));
      filters?.provinceIds?.forEach(id => params.append('provinceIds', id));
      filters?.municipalityIds?.forEach(id => params.append('municipalityIds', id));
      filters?.barangayIds?.forEach(id => params.append('barangayIds', id));
      
      const response = await fetch(`/barangay-coverages?${params.toString()}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      return response.json();
    }
  });
}

// Query to get single coverage
export function useBarangayCoverage(id: string) {
  return useQuery({
    queryKey: ['barangay-coverage', id],
    queryFn: async () => {
      const response = await fetch(`/barangay-coverages/${id}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      return response.json();
    }
  });
}

// Mutation to create coverage
export function useCreateBarangayCoverage() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async (data: CreateCoverageDto) => {
      const response = await fetch('/barangay-coverages', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(data)
      });
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['barangay-coverages'] });
    }
  });
}

// Mutation to update coverage
export function useUpdateBarangayCoverage() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async ({ id, data }: { id: string; data: UpdateCoverageDto }) => {
      const response = await fetch(`/barangay-coverages/${id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(data)
      });
      
      if (!response.ok) throw new Error('Update failed');
      return response;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['barangay-coverages'] });
      queryClient.invalidateQueries({ queryKey: ['barangay-coverage', variables.id] });
    }
  });
}

// Mutation to delete coverage
export function useDeleteBarangayCoverage() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await fetch(`/barangay-coverages/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!response.ok) throw new Error('Delete failed');
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['barangay-coverages'] });
    }
  });
}
```

---

## Best Practices

1. **Use the list endpoint for browsing/filtering**: The `GET /barangay-coverages` endpoint returns lightweight data without relationships, making it ideal for tables and lists.

2. **Use the single record endpoint for details**: The `GET /barangay-coverages/{id}` endpoint includes relationship data (barangay name, code) for detail views.

3. **Leverage computed fields**: The API automatically computes `combinedPercentage`, `isWiredCovered`, and `isWirelessCovered` - no need to calculate these on the client.

4. **Handle 204 responses**: Update and Delete operations return `204 No Content` on success with no response body.

5. **Check the `succeeded` field**: Always check `result.succeeded` in the response before accessing `result.data`.

6. **Filter efficiently**: Use the most specific filter available to minimize data transfer.

7. **Cache appropriately**: Coverage data changes infrequently, so consider implementing client-side caching with appropriate invalidation strategies.

---

## Migration from Legacy Endpoints

The following legacy endpoints are deprecated and will be removed in a future version:

| Legacy Endpoint | New Endpoint |
|----------------|--------------|
| `PATCH /barangay-coverages/barangays/{barangayId}` | `PUT /barangay-coverages/{id}` |
| `GET /barangay-coverages/barangays/{barangayId}` | `GET /barangay-coverages/{id}` |
| `DELETE /barangay-coverages/barangay/{barangayId}` | `DELETE /barangay-coverages/{id}` |
| `GET /barangay-coverages/regions/{regionId}` | `GET /barangay-coverages?regionIds={regionId}` |

**Key Changes:**
1. Use coverage record ID instead of barangay ID
2. Use PUT instead of PATCH for updates
3. Update returns 204 No Content instead of the updated object
4. Delete returns 204 No Content instead of a boolean result
5. List endpoint uses query parameters instead of URL path parameters

---

## Support

For issues or questions, please contact the NTC QoS development team or file an issue in the project repository.

**Version:** 1.0  
**Last Updated:** December 29, 2024
