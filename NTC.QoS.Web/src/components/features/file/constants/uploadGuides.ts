export interface UploadGuideInfo {
  type: string;
  title: string;
  pdfFileName: string;
  pdfPath: string;
  rules: string[];
}

export const UPLOAD_GUIDES: Record<string, UploadGuideInfo> = {
  Speedtest: {
    type: "Speedtest",
    title: "Speedtest Upload Guidelines",
    pdfFileName: "Speedtest_Uploader_Guidelines.pdf",
    pdfPath: "/guides/Speedtest_Uploader_Guidelines.pdf",
    rules: [
      "Mandatory fields: timestamp, upload, download, operator, lat, lon.",
      "Timestamps in Philippine Standard Time (PHT, UTC+8) with Month first (MM/DD/YYYY, time).",
      "Do NOT manually convert or subtract 8 hours from timestamps.",
      "Longitude header must be strictly named 'lon' (not 'lng' or 'long').",
      "Set nbc = TRUE for unserved areas or missing readings.",
    ],
  },
  NAP: {
    type: "NAP",
    title: "NAP Upload Guidelines",
    pdfFileName: "NAP_Uploader_Guidelines.pdf",
    pdfPath: "/guides/NAP_Uploader_Guidelines.pdf",
    rules: [
      "All 7 columns are mandatory: Provider, Barangay, Municipality, Province, Region, Lat, Lng.",
      "Provider must match registered Wired Service Providers (e.g. PLDT, GLOBE, CONVERGE).",
      "Clean WGS84 decimal coordinates (Lat between 4.0°-21.5°, Lng between 116.0°-127.0°).",
      "Coordinates must not be swapped (Lat in Col F, Lng in Col G).",
    ],
  },
  Cell: {
    type: "Cell",
    title: "Cell Site Upload Guidelines",
    pdfFileName: "Cell_Uploader_Guidelines.pdf",
    pdfPath: "/guides/Cell_Uploader_Guidelines.pdf",
    rules: [
      "Specialized 3-row composite header; data rows begin strictly on Row 4.",
      "Active Technology Rule: At least 1 band must be TRUE (or valid CF/BW) for coverage analysis.",
      "Multi-sector Azimuth/Tilt: Separate with semicolons (e.g. '0; 120; 240' and '2; 2; 2').",
      "Azimuth and Beam Tilt count must strictly match 1:1, or sector creation is skipped.",
      "Provider must match registered wireless operators (DITO, GLOBE, SMART).",
    ],
  },
  CellularCoverage: {
    type: "CellularCoverage",
    title: "Cellular Coverage Upload Guidelines (WIP)",
    pdfFileName: "Cellular_Coverage_Uploader_Guidelines.pdf",
    pdfPath: "/guides/Cellular_Coverage_Uploader_Guidelines.pdf",
    rules: [
      "⚠️ UNDER DEVELOPMENT: Cellular Coverage upload is currently a work-in-progress.",
      "5 mandatory columns: ServiceProvider, Network, Lat, Lng, Strength.",
      "Preserve negative sign for Strength in dBm (e.g. -85.5, no text units).",
      "Network must match an existing pre-configured network name under that provider.",
      "Clean WGS84 decimal coordinates without degree symbols.",
    ],
  },
  Cellular: {
    type: "Cellular",
    title: "Cellular Coverage Upload Guidelines (WIP)",
    pdfFileName: "Cellular_Coverage_Uploader_Guidelines.pdf",
    pdfPath: "/guides/Cellular_Coverage_Uploader_Guidelines.pdf",
    rules: [
      "⚠️ UNDER DEVELOPMENT: Cellular Coverage upload is currently a work-in-progress.",
      "5 mandatory columns: ServiceProvider, Network, Lat, Lng, Strength.",
      "Preserve negative sign for Strength in dBm (e.g. -85.5, no text units).",
      "Network must match an existing pre-configured network name under that provider.",
      "Clean WGS84 decimal coordinates without degree symbols.",
    ],
  },
  WirelessValidation: {
    type: "WirelessValidation",
    title: "Wireless Validation Upload Guidelines",
    pdfFileName: "Wireless_Validation_Uploader_Guidelines.pdf",
    pdfPath: "/guides/Wireless_Validation_Uploader_Guidelines.pdf",
    rules: [
      "Download template with target Province & Municipality selected.",
      "Do NOT edit Cell A1 (Location Header: <Municipality>, <Province>, <Region>).",
      "Survey data rows begin strictly on Row 5.",
      "Band columns accept booleans (TRUE / 1 / YES vs FALSE / 0 / NO / blank).",
    ],
  },
};
