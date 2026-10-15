/**
 * Generates a standardized report filename matching the backend format:
 * {ReportType}_{Scope}_{LocationName}_{YYYYMMDD}_{HHMMSS}.{extension}
 * 
 * Examples:
 * - ConnectivityReport_National_Capital_Region_(NCR)_20260119_061555.pdf
 * - WiredBroadbandReport_Region_I_(Ilocos_Region)_20260115_110932.xlsx
 */

interface GenerateReportFilenameParams {
  reportType: 'WiredBroadband' | 'WirelessBroadband' | 'Connectivity' | 'ConnectivityGap';
  scope: 'National' | 'Region' | 'Province' | 'Municipality';
  locationName?: string;
  extension: 'pdf' | 'xlsx';
}

export function generateReportFilename(params: GenerateReportFilenameParams): string {
  const { reportType, scope, locationName, extension } = params;
  
  // Generate timestamp in YYYYMMDD_HHMMSS format
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');
  const seconds = String(now.getSeconds()).padStart(2, '0');
  
  const dateStr = `${year}${month}${day}`;
  const timeStr = `${hours}${minutes}${seconds}`;
  
  // Build location part
  let locationPart: string = scope;
  if (locationName) {
    // Replace spaces with underscores and handle parentheses
    const formattedLocation = locationName.replace(/ /g, '_');
    locationPart = formattedLocation;
  }
  
  // Build filename
  const filename = `${reportType}Report_${locationPart}_${dateStr}_${timeStr}.${extension}`;
  
  return filename;
}

/**
 * Determines the scope and location name based on selected filters
 */
export function determineReportScope(
  regionId: string,
  provinceId: string,
  municipalityId: string,
  regionName?: string,
  provinceName?: string,
  municipalityName?: string
): { scope: 'National' | 'Region' | 'Province' | 'Municipality'; locationName?: string } {
  if (municipalityId !== 'all' && municipalityName) {
    return { scope: 'Municipality', locationName: municipalityName };
  }
  
  if (provinceId !== 'all' && provinceName) {
    return { scope: 'Province', locationName: provinceName };
  }
  
  if (regionId !== 'all' && regionName) {
    return { scope: 'Region', locationName: regionName };
  }
  
  return { scope: 'National', locationName: undefined };
}
