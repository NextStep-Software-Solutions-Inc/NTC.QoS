# Speedtest Analytics Dashboard - API Integration

## Overview
This document describes the integration of the Speedtest Analytics Dashboard with the `/analytics/speedtest-analytics` API endpoint. The dashboard has been refactored to fetch pre-calculated analytics from the backend instead of performing client-side calculations.

## Changes Made

### 1. **New API Types** (`types/analytics-api.ts`)
Created TypeScript interfaces matching the API response structure:

- `SpeedtestAnalyticsApiResponse` - Root response wrapper
- `SpeedtestAnalyticsData` - Main analytics data container
- `AnalyticsOverview` - Summary metrics
- `ProviderPerformance` - Provider-level statistics
- `SignalQualityDistribution` - Quality breakdown
- `ProviderRanking` - Ranked provider list
- `SpeedtestAnalyticsQueryParams` - Query filters (barangays, municipalities, provinces, regions, date range)

### 2. **API Handler** (`handlers/speedtest-analytics-handler.ts`)
Created a query blueprint using the project's handler pattern:

```typescript
export const speedtestAnalyticsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  SpeedtestAnalyticsApiResponse,
  SpeedtestAnalyticsQueryParams
> = {
  endpoint: "/analytics/speedtest-analytics",
  key: ({ url, pathParams, queryParams }) =>
    ["speedtest-analytics", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<SpeedtestAnalyticsApiResponse>(finalUrl);
  },
};
```

### 3. **React Query Hook** (`hooks/useSpeedtestAnalytics.ts`)
Created a custom hook using `createQueryHookFromTemplate`:

```typescript
export const useSpeedtestAnalytics = createQueryHookFromTemplate(speedtestAnalyticsRequest);
```

### 4. **Component Refactor** (`SpeedtestAnalyticsPanel.tsx`)

#### Props Changed
**Before:**
```typescript
interface SpeedtestAnalyticsPanelProps {
  speedtests: SpeedTest[];
  // ...
}
```

**After:**
```typescript
interface SpeedtestAnalyticsPanelProps {
  queryParams?: SpeedtestAnalyticsQueryParams;
  // ...
}
```

#### Removed Client-Side Calculation
- Deleted the entire `calculateAnalytics()` function (~300 lines)
- Removed all manual aggregation, quality scoring, and statistics calculation logic

#### Added API Integration
- Uses `useSpeedtestAnalytics` hook to fetch data
- Maps API response to existing UI components
- Handles loading, error, and empty states

#### Data Mapping
- `overview` → Key metrics cards and insights badges
- `providerPerformance` → Provider performance bar chart
- `signalQualityDistribution` → Signal quality pie chart
- `providerRankings` → Provider rankings grid

### 5. **New UI States**

#### Loading State
```tsx
<div className="flex items-center justify-center gap-3 ...">
  <Loader2 className="h-6 w-6 animate-spin" />
  <span>Loading analytics...</span>
</div>
```

#### Error State
```tsx
<div className="flex items-center gap-3 rounded-xl border border-red-200 ...">
  <AlertCircle className="h-5 w-5" />
  <div>
    <p>Failed to load analytics</p>
    <p>{error message}</p>
  </div>
</div>
```

## API Endpoint Details

### Endpoint
```
GET /analytics/speedtest-analytics
```

### Query Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `barangayIds` | `string[]` | Filter by barangay IDs |
| `municipalityIds` | `string[]` | Filter by municipality IDs |
| `provinceIds` | `string[]` | Filter by province IDs |
| `regionIds` | `string[]` | Filter by region IDs |
| `startDate` | `string` | Start date (ISO format) |
| `endDate` | `string` | End date (ISO format) |

### Response Structure
```json
{
  "data": {
    "overview": {
      "totalTests": 0,
      "averageDownloadMbps": 0,
      "averageUploadMbps": 0,
      "medianDownloadMbps": 0,
      "medianUploadMbps": 0,
      "averagePingMs": 0,
      "providerCount": 0,
      "coverageAreaKm2": 0,
      "performanceRating": "string",
      "bestProvider": "string",
      "competitionLevel": "string"
    },
    "providerPerformance": [...],
    "signalQualityDistribution": {...},
    "providerRankings": [...]
  },
  "succeeded": true,
  "message": "string",
  "errors": {...}
}
```

## Usage Example

### Basic Usage
```tsx
<SpeedtestAnalyticsPanel 
  isCollapsed={false}
  onCollapsedChange={setCollapsed}
/>
```

### With Filters
```tsx
<SpeedtestAnalyticsPanel 
  queryParams={{
    regionIds: ["NCR"],
    startDate: "2025-01-01",
    endDate: "2025-01-31"
  }}
  isCollapsed={false}
  onCollapsedChange={setCollapsed}
/>
```

**Note**: The component automatically handles the `pathParams: {}` internally when calling the hook.

## Benefits of API Integration

1. **Performance**: No client-side computation overhead
2. **Consistency**: Analytics calculated once on the backend
3. **Scalability**: Can handle large datasets without client memory issues
4. **Maintainability**: Analytics logic centralized in backend
5. **Caching**: React Query provides automatic caching and background refetching

## File Structure
```
src/components/features/speedtest/analytics/
├── handlers/
│   ├── index.ts
│   └── speedtest-analytics-handler.ts
├── hooks/
│   ├── index.ts
│   └── useSpeedtestAnalytics.ts
├── types/
│   ├── index.ts
│   └── analytics-api.ts
└── SpeedtestAnalyticsPanel.tsx
```

## Migration Notes

- The component no longer requires a `speedtests` prop
- All analytics are now server-calculated
- The "Recommendations" section was removed (not in API response)
- Quality distribution now uses explicit count/percentage from API
- Provider rankings use the `badge` field from API response

## TypeScript Improvements

- All map functions have explicit type annotations
- No implicit `any` types
- Full type safety from API to UI
- Proper typing for query parameters

## Change Log

### Added
- ✅ API response types (`analytics-api.ts`)
- ✅ API handler using blueprint pattern
- ✅ React Query hook with caching
- ✅ Loading and error states
- ✅ Support for query parameters (filters)

### Changed
- ✅ Component props (accepts `queryParams` instead of `speedtests`)
- ✅ Data source (API instead of client calculation)
- ✅ Chart data mapping to use API field names

### Removed
- ✅ `calculateAnalytics()` function
- ✅ Client-side aggregation logic
- ✅ Recommendations section
- ✅ Dependency on `SpeedTest[]` prop

---

**Last Updated**: January 16, 2026
**Author**: GitHub Copilot
**Status**: ✅ Complete
