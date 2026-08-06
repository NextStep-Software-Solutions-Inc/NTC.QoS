# Wireless Validation Template Download Update

## Change Summary
Updated the Wireless Validation template download feature to use the new municipality-based API endpoint and enhanced the UI with municipality selection.

## Changes Made

### 1. Updated API Endpoint
- **Old Endpoint:** `GET /files/download/{type}` (generic for all file types)
- **New Endpoint:** `GET /files/download/wireless-validation/{municipalityId}` (municipality-specific)
- The new endpoint requires a `municipalityId` (UUID) path parameter

### 2. UI Enhancements in UploadFileForm

#### Added Municipality Selector
- Added a dropdown to select a municipality before downloading the template
- Municipality selector only appears for WirelessValidation file type
- Dropdown is populated with municipalities from the current region
- Includes loading state while fetching municipalities
- Uses ChevronDown icon for consistent styling with other dropdowns

#### Enhanced Download Logic
- Download link is now conditionally generated based on file type:
  - **WirelessValidation:** Uses `/files/download/wireless-validation/{municipalityId}`
  - **Other types:** Uses `/files/download/{type}` (unchanged)
- Download button is disabled until a municipality is selected (for WirelessValidation)
- Provides helpful feedback text when municipality selection is required

### 3. Code Changes

**File Modified:** [src/components/features/file/forms/UploadFileForm.tsx](src/components/features/file/forms/UploadFileForm.tsx)

#### New Imports
```typescript
import { ChevronDown } from "lucide-react";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getMunicipalitiesRequest } from "../../municipality/handlers/getMunicipalitiesRequest";
```

#### New State & Hooks
```typescript
const [selectedMunicipalityId, setSelectedMunicipalityId] = useState<string>("");
const useGetMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
```

#### Updated Type Definition
```typescript
type: "Speedtest" | "NAP" | "Cell" | "Cellular" | "CellularCoverage" | "WirelessValidation";
```

#### New Helper Functions
- `getDownloadUrl()`: Dynamically generates the download URL based on file type
- `isDownloadEnabled`: Checks if download should be enabled (requires municipality for WirelessValidation)

## User Experience

### Before
- All file types used the same generic download endpoint
- No municipality context for Wireless Validation templates

### After
1. User opens the upload dialog for Wireless Validation
2. User sees a municipality dropdown (required)
3. User selects a municipality from the list
4. Download button becomes active
5. User clicks download and receives a municipality-specific template
6. Template includes pre-populated data relevant to the selected municipality

## Technical Details

### API Integration
- Fetches municipalities using `getMunicipalitiesRequest` filtered by current region
- Page size: 1000 municipalities per request
- Municipalities are sorted and displayed by name

### Styling
- Follows existing design patterns from the project
- Uses Tailwind CSS utility classes for consistency
- Gradient background for download section (blue to cyan)
- Proper focus states and accessibility support
- Loading states for better UX

### Type Safety
- All TypeScript types are properly defined
- Municipality type imported from existing municipality feature
- Props interface updated to include WirelessValidation

## Testing Recommendations

1. **Functionality Testing:**
   - Verify municipality dropdown loads correctly for the region
   - Test download with different municipalities
   - Ensure download is disabled without municipality selection
   - Test other file types (NAP, Speedtest, etc.) still work

2. **Error Scenarios:**
   - No municipalities available in region
   - Network error while fetching municipalities
   - Invalid municipality ID

3. **UI/UX Testing:**
   - Dropdown styling matches other selectors
   - Loading state displays correctly
   - Disabled state is visually clear
   - Responsive design on mobile devices

## Dependencies
- No new dependencies required
- Uses existing municipality handlers and types
- Leverages existing query hooks pattern

## Backend Requirements
The backend must provide:
1. `GET /files/download/wireless-validation/{municipalityId}` endpoint
2. Return municipality-specific template file
3. Accept UUID format for municipalityId parameter

## Related Files
- [src/components/features/file/forms/UploadFileForm.tsx](src/components/features/file/forms/UploadFileForm.tsx) - Main implementation
- [src/components/features/municipality/handlers/getMunicipalitiesRequest.ts](src/components/features/municipality/handlers/getMunicipalitiesRequest.ts) - Municipality data fetching
- [swagger.json](swagger.json) - API documentation (line 4596)

---

**Date:** January 26, 2026  
**Branch:** hotfix/update-download-wv-api  
**Status:** ✅ Complete
