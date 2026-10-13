# NTC QoS Data Pre-processing & Upload Manuals (SOP)

This directory contains the official Standard Operating Procedure (SOP) upload manuals and sanitization guidelines for all supported data import file types in the National Telecommunications Commission (NTC) Quality of Service (QoS) portal.

## Document Directory & Downloads

| # | Data Type | Module / Tab | Markdown Manual | PDF Document | Key Requirement Highlights |
|---|:---|:---|:---|:---|:---|
| 1 | **Speedtest** | Speedtest Import | [speedtest_uploader_guidelines.md](file:///docs/upload-guides/speedtest_uploader_guidelines.md) | [Speedtest_Uploader_Guidelines.pdf](file:///docs/upload-guides/Speedtest_Uploader_Guidelines.pdf) | `MM/DD/YYYY, time` timestamps, clean numeric upload/download speeds, `nbc` logic, operator dropdown. |
| 2 | **NAP** | Fixed Broadband (NAP) | [nap_uploader_guidelines.md](file:///docs/upload-guides/nap_uploader_guidelines.md) | [NAP_Uploader_Guidelines.pdf](file:///docs/upload-guides/NAP_Uploader_Guidelines.pdf) | Valid wired ISP provider, clean WGS84 decimal coordinates (`Lat`, `Lng`), complete geographic hierarchy. |
| 3 | **Cell Site** | Cellular Infrastructure | [cell_uploader_guidelines.md](file:///docs/upload-guides/cell_uploader_guidelines.md) | [Cell_Uploader_Guidelines.pdf](file:///docs/upload-guides/Cell_Uploader_Guidelines.pdf) | 3-row composite header, data starts on Row 4, **active technology rule** (at least 1 band must be `TRUE`). |
| 4 | **Cellular Coverage** | Signal Heatmaps | [cellular_coverage_uploader_guidelines.md](file:///docs/upload-guides/cellular_coverage_uploader_guidelines.md) | [Cellular_Coverage_Uploader_Guidelines.pdf](file:///docs/upload-guides/Cellular_Coverage_Uploader_Guidelines.pdf) | Preserved negative sign on signal strength (`dBm`), registered network names, spatial deduplication. |
| 5 | **Wireless Validation** | LGU Ground Survey | [wireless_validation_uploader_guidelines.md](file:///docs/upload-guides/wireless_validation_uploader_guidelines.md) | [Wireless_Validation_Uploader_Guidelines.pdf](file:///docs/upload-guides/Wireless_Validation_Uploader_Guidelines.pdf) | Municipality-specific downloaded template, **Cell A1 Location Header**, survey data begins on Row 5, boolean bands. |

---

## General Rules Across All Imports

1. **Official Templates Only**: Never construct upload spreadsheets from scratch. Always download the latest official Excel template (`.xlsx`) directly from the QoS portal.
2. **Clean Numbers**: Remove all text suffixes (`Mbps`, `dBm`, `deg`, `m`), commas, and degree symbols from numeric fields.
3. **No Placeholders**: Leave unmeasured or optional cells completely blank; do not write `"N/A"`, `"---"`, `"null"`, or `"0"` (unless zero is an actual valid measured value).
4. **Coordinate Accuracy**: Always verify that Latitude (`Lat`) and Longitude (`Lng`) are in WGS84 decimal format and not swapped.
