# Barangay Coverage API - Frontend Integration Guide

## Overview

This document describes the new Barangay Coverage Management API endpoints that manage broadband coverage data for individual Barangays. These endpoints allow regional admins to track wired FTTH and wireless broadband coverage percentages.

**Base URL:** `/api/barangay-coverage`

---

## Authentication & Authorization

All endpoints require authentication via Bearer token and specific permissions:

- **View Coverage:** `barangay_coverage.view`
- **Create Coverage:** `barangay_coverage.create`
- **Update Coverage:** `barangay_coverage.update`

Include the JWT token in the Authorization header:
```
Authorization: Bearer <your_jwt_token>
```

---

## Data Model

### BarangayCoverageDto

Response object returned by all endpoints:

```typescript
interface BarangayCoverageDto {
  id: string;                      // UUID of the coverage record
  barangayId: string;              // UUID of the barangay
  barangayName: string;            // Name of the barangay
  barangayCode: string;            // Code of the barangay
  wiredFtthPercentage: number;    // 0-100, wired FTTH coverage
  wirelessPercentage: number;      // 0-100, wireless coverage
  combinedPercentage: number;      // Computed: min(wired + wireless, 100)
  isWiredCovered: boolean;         // true if wiredFtthPercentage == 100
  isWirelessCovered: boolean;      // true if wirelessPercentage == 100
  updatedAt: string;               // ISO 8601 datetime
  updatedBy: string;               // Username who last updated
}
```

### CreateBarangayCoverageDto

Request body for creating new coverage:

```typescript
interface CreateBarangayCoverageDto {
  barangayId: string;              // Required: UUID of the barangay
  wiredFtthPercentage: number;    // Required: 0-100
  wirelessPercentage: number;      // Required: 0-100
}
```

### UpdateBarangayCoverageDto

Request body for updating existing coverage:

```typescript
interface UpdateBarangayCoverageDto {
  wiredFtthPercentage: number;    // Required: 0-100
  wirelessPercentage: number;      // Required: 0-100
}
```

### ApiResponse Wrapper

All responses are wrapped in this structure:

```typescript
interface ApiResponse<T> {
  succeeded: boolean;              // true if request was successful
  message: string;                 // Human-readable message
  data?: T;                        // Response data (present on success)
}
```

---

## Endpoints

### 1. Create Coverage Data

Creates coverage data for a barangay that doesn't have coverage tracking yet.

**Endpoint:** `POST /api/barangay-coverage`

**Permission:** `barangay_coverage.create`

**Request Body:**
```json
{
  "barangayId": "550e8400-e29b-41d4-a716-446655440000",
  "wiredFtthPercentage": 75.5,
  "wirelessPercentage": 85.0
}
```

**Success Response (200 OK):**
```json
{
  "succeeded": true,
  "message": "Barangay coverage created successfully.",
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "barangayId": "550e8400-e29b-41d4-a716-446655440000",
    "barangayName": "San Jose",
    "barangayCode": "012345",
    "wiredFtthPercentage": 75.5,
    "wirelessPercentage": 85.0,
    "combinedPercentage": 100.0,
    "isWiredCovered": false,
    "isWirelessCovered": false,
    "updatedAt": "2025-12-28T04:30:00Z",
    "updatedBy": "admin@example.com"
  }
}
```

**Error Responses:**

- **400 Bad Request** - Barangay not found:
```json
{
  "succeeded": false,
  "message": "Barangay not found."
}
```

- **400 Bad Request** - Coverage already exists:
```json
{
  "succeeded": false,
  "message": "Coverage data already exists for this barangay. Use UPDATE instead."
}
```

- **400 Bad Request** - Invalid data:
```json
{
  "succeeded": false,
  "message": "WiredFtthPercentage must be between 0 and 100"
}
```

**TypeScript Example:**
```typescript
const createCoverage = async (barangayId: string, wired: number, wireless: number) => {
  const response = await fetch('/api/barangay-coverage', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      barangayId,
      wiredFtthPercentage: wired,
      wirelessPercentage: wireless
    })
  });
  
  const result: ApiResponse<BarangayCoverageDto> = await response.json();
  return result;
};
```

---

### 2. Update Coverage Data

Updates existing coverage data for a barangay.

**Endpoint:** `PATCH /api/barangay-coverage/barangay/{barangayId}`

**Permission:** `barangay_coverage.update`

**URL Parameters:**
- `barangayId` (string, required): UUID of the barangay

**Request Body:**
```json
{
  "wiredFtthPercentage": 80.0,
  "wirelessPercentage": 90.0
}
```

**Success Response (200 OK):**
```json
{
  "succeeded": true,
  "message": "Barangay coverage updated successfully.",
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "barangayId": "550e8400-e29b-41d4-a716-446655440000",
    "barangayName": "San Jose",
    "barangayCode": "012345",
    "wiredFtthPercentage": 80.0,
    "wirelessPercentage": 90.0,
    "combinedPercentage": 100.0,
    "isWiredCovered": false,
    "isWirelessCovered": false,
    "updatedAt": "2025-12-28T05:00:00Z",
    "updatedBy": "admin@example.com"
  }
}
```

**Error Responses:**

- **400 Bad Request** - Coverage not found:
```json
{
  "succeeded": false,
  "message": "Coverage data not found for this barangay. Use CREATE instead."
}
```

**TypeScript Example:**
```typescript
const updateCoverage = async (barangayId: string, wired: number, wireless: number) => {
  const response = await fetch(`/api/barangay-coverage/barangay/${barangayId}`, {
    method: 'PATCH',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      wiredFtthPercentage: wired,
      wirelessPercentage: wireless
    })
  });
  
  const result: ApiResponse<BarangayCoverageDto> = await response.json();
  return result;
};
```

---

### 3. Get Coverage by Barangay ID

Retrieves coverage data for a specific barangay.

**Endpoint:** `GET /api/barangay-coverage/barangay/{barangayId}`

**Permission:** `barangay_coverage.view`

**URL Parameters:**
- `barangayId` (string, required): UUID of the barangay

**Success Response (200 OK):**
```json
{
  "succeeded": true,
  "message": "Coverage data retrieved successfully.",
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "barangayId": "550e8400-e29b-41d4-a716-446655440000",
    "barangayName": "San Jose",
    "barangayCode": "012345",
    "wiredFtthPercentage": 80.0,
    "wirelessPercentage": 90.0,
    "combinedPercentage": 100.0,
    "isWiredCovered": false,
    "isWirelessCovered": false,
    "updatedAt": "2025-12-28T05:00:00Z",
    "updatedBy": "admin@example.com"
  }
}
```

**Error Responses:**

- **400 Bad Request** - Coverage not found:
```json
{
  "succeeded": false,
  "message": "Coverage data not found for this barangay."
}
```

**TypeScript Example:**
```typescript
const getCoverageByBarangay = async (barangayId: string) => {
  const response = await fetch(`/api/barangay-coverage/barangay/${barangayId}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const result: ApiResponse<BarangayCoverageDto> = await response.json();
  return result;
};
```

---

### 4. Get Coverage by Region

Retrieves all coverage data for barangays in a specific region.

**Endpoint:** `GET /api/barangay-coverage/region/{regionId}`

**Permission:** `barangay_coverage.view`

**URL Parameters:**
- `regionId` (string, required): UUID of the region

**Success Response (200 OK):**
```json
{
  "succeeded": true,
  "message": "Retrieved 150 coverage records for region NCR.",
  "data": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "barangayId": "550e8400-e29b-41d4-a716-446655440000",
      "barangayName": "San Jose",
      "barangayCode": "012345",
      "wiredFtthPercentage": 80.0,
      "wirelessPercentage": 90.0,
      "combinedPercentage": 100.0,
      "isWiredCovered": false,
      "isWirelessCovered": false,
      "updatedAt": "2025-12-28T05:00:00Z",
      "updatedBy": "admin@example.com"
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440002",
      "barangayId": "550e8400-e29b-41d4-a716-446655440001",
      "barangayName": "Santa Cruz",
      "barangayCode": "012346",
      "wiredFtthPercentage": 100.0,
      "wirelessPercentage": 100.0,
      "combinedPercentage": 100.0,
      "isWiredCovered": true,
      "isWirelessCovered": true,
      "updatedAt": "2025-12-28T04:45:00Z",
      "updatedBy": "admin@example.com"
    }
  ]
}
```

**Error Responses:**

- **400 Bad Request** - Region not found:
```json
{
  "succeeded": false,
  "message": "Region not found."
}
```

**TypeScript Example:**
```typescript
const getCoverageByRegion = async (regionId: string) => {
  const response = await fetch(`/api/barangay-coverage/region/${regionId}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const result: ApiResponse<BarangayCoverageDto[]> = await response.json();
  return result;
};
```

---

## Computed Fields

The API automatically computes and returns the following fields:

### combinedPercentage
- **Formula:** `Math.min(wiredFtthPercentage + wirelessPercentage, 100)`
- **Purpose:** Shows the total coverage, capped at 100%
- **Example:** If wired is 75% and wireless is 50%, combined is 100% (not 125%)

### isWiredCovered
- **Formula:** `wiredFtthPercentage === 100`
- **Purpose:** Boolean flag indicating full wired FTTH coverage
- **Usage:** Use this for filtering/reporting "covered" barangays for wired

### isWirelessCovered
- **Formula:** `wirelessPercentage === 100`
- **Purpose:** Boolean flag indicating full wireless coverage
- **Usage:** Use this for filtering/reporting "covered" barangays for wireless

---

## Validation Rules

All percentage fields must follow these rules:

1. **Range:** Must be between 0 and 100 (inclusive)
2. **Type:** Decimal numbers are supported (e.g., 75.5)
3. **Required:** Both `wiredFtthPercentage` and `wirelessPercentage` are required in all requests

**Invalid Examples:**
```json
{
  "wiredFtthPercentage": -10,    // ❌ Less than 0
  "wirelessPercentage": 150      // ❌ Greater than 100
}
```

**Valid Examples:**
```json
{
  "wiredFtthPercentage": 0,      // ✅ Minimum value
  "wirelessPercentage": 100      // ✅ Maximum value
}

{
  "wiredFtthPercentage": 75.5,   // ✅ Decimal supported
  "wirelessPercentage": 82.3     // ✅ Decimal supported
}
```

---

## Error Handling

All endpoints follow standard HTTP status codes:

| Status Code | Meaning |
|------------|---------|
| 200 | Success |
| 400 | Bad Request (validation error, not found, etc.) |
| 401 | Unauthorized (missing or invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 500 | Internal Server Error |

**Error Response Format:**
```json
{
  "succeeded": false,
  "message": "Human-readable error message"
}
```

---

## Complete React/TypeScript Example

```typescript
import { useState, useEffect } from 'react';

interface BarangayCoverageDto {
  id: string;
  barangayId: string;
  barangayName: string;
  barangayCode: string;
  wiredFtthPercentage: number;
  wirelessPercentage: number;
  combinedPercentage: number;
  isWiredCovered: boolean;
  isWirelessCovered: boolean;
  updatedAt: string;
  updatedBy: string;
}

interface ApiResponse<T> {
  succeeded: boolean;
  message: string;
  data?: T;
}

const API_BASE_URL = '/api/barangay-coverage';

class BarangayCoverageService {
  constructor(private token: string) {}

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    return response.json();
  }

  async create(
    barangayId: string, 
    wired: number, 
    wireless: number
  ): Promise<ApiResponse<BarangayCoverageDto>> {
    return this.request('', {
      method: 'POST',
      body: JSON.stringify({
        barangayId,
        wiredFtthPercentage: wired,
        wirelessPercentage: wireless,
      }),
    });
  }

  async update(
    barangayId: string, 
    wired: number, 
    wireless: number
  ): Promise<ApiResponse<BarangayCoverageDto>> {
    return this.request(`/barangay/${barangayId}`, {
      method: 'PATCH',
      body: JSON.stringify({
        wiredFtthPercentage: wired,
        wirelessPercentage: wireless,
      }),
    });
  }

  async getByBarangay(
    barangayId: string
  ): Promise<ApiResponse<BarangayCoverageDto>> {
    return this.request(`/barangay/${barangayId}`);
  }

  async getByRegion(
    regionId: string
  ): Promise<ApiResponse<BarangayCoverageDto[]>> {
    return this.request(`/region/${regionId}`);
  }
}

// Usage Example
export const BarangayCoverageManager: React.FC = () => {
  const [service] = useState(() => new BarangayCoverageService('your-jwt-token'));
  const [coverage, setCoverage] = useState<BarangayCoverageDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadCoverage = async (barangayId: string) => {
    setLoading(true);
    setError(null);
    
    try {
      const result = await service.getByBarangay(barangayId);
      
      if (result.succeeded && result.data) {
        setCoverage(result.data);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError('Failed to load coverage data');
    } finally {
      setLoading(false);
    }
  };

  const updateCoverage = async (
    barangayId: string, 
    wired: number, 
    wireless: number
  ) => {
    setLoading(true);
    setError(null);
    
    try {
      const result = await service.update(barangayId, wired, wireless);
      
      if (result.succeeded && result.data) {
        setCoverage(result.data);
        alert('Coverage updated successfully!');
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError('Failed to update coverage data');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}
      
      {coverage && (
        <div>
          <h2>{coverage.barangayName}</h2>
          <p>Code: {coverage.barangayCode}</p>
          <p>Wired FTTH: {coverage.wiredFtthPercentage}%</p>
          <p>Wireless: {coverage.wirelessPercentage}%</p>
          <p>Combined: {coverage.combinedPercentage}%</p>
          <p>Wired Covered: {coverage.isWiredCovered ? 'Yes' : 'No'}</p>
          <p>Wireless Covered: {coverage.isWirelessCovered ? 'Yes' : 'No'}</p>
          <p>Last Updated: {new Date(coverage.updatedAt).toLocaleString()}</p>
          <p>Updated By: {coverage.updatedBy}</p>
        </div>
      )}
    </div>
  );
};
```

---

## Important Notes

### Reporting Scope Changes

⚠️ **Breaking Change:** Reports and analytics will now **only include barangays** that have a record in the `BarangayCoverage` table. 

- Previously, all 42,000+ barangays were included in reports
- Now, only barangays explicitly tracked in `BarangayCoverage` are reported
- Regional admins must create coverage entries for barangays they manage

### Coverage Logic

The system uses **strict 100% logic** for coverage flags:

- `isWiredCovered = true` **only when** `wiredFtthPercentage === 100`
- `isWirelessCovered = true` **only when** `wirelessPercentage === 100`
- Use these boolean flags (not percentages) when determining "covered" status in reports

### One Record Per Barangay

- Each barangay can have **only one** coverage record (enforced by unique index)
- Use `POST` to create the first record
- Use `PATCH` to update existing records
- Attempting to create a duplicate will return an error directing you to use UPDATE

---

## Postman Collection

Import this collection to test the endpoints:

```json
{
  "info": {
    "name": "Barangay Coverage API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Create Coverage",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"barangayId\": \"{{barangayId}}\",\n  \"wiredFtthPercentage\": 75.5,\n  \"wirelessPercentage\": 85.0\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        },
        "url": {
          "raw": "{{baseUrl}}/api/barangay-coverage",
          "host": ["{{baseUrl}}"],
          "path": ["api", "barangay-coverage"]
        }
      }
    },
    {
      "name": "Update Coverage",
      "request": {
        "method": "PATCH",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"wiredFtthPercentage\": 80.0,\n  \"wirelessPercentage\": 90.0\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        },
        "url": {
          "raw": "{{baseUrl}}/api/barangay-coverage/barangay/{{barangayId}}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "barangay-coverage", "barangay", "{{barangayId}}"]
        }
      }
    },
    {
      "name": "Get Coverage by Barangay",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/barangay-coverage/barangay/{{barangayId}}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "barangay-coverage", "barangay", "{{barangayId}}"]
        }
      }
    },
    {
      "name": "Get Coverage by Region",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/barangay-coverage/region/{{regionId}}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "barangay-coverage", "region", "{{regionId}}"]
        }
      }
    }
  ]
}
```

---

## Questions or Issues?

For questions about this API, please contact the backend development team or refer to the main API documentation.

**Last Updated:** December 28, 2025
**API Version:** 1.0
