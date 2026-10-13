# NAP Data Pre-processing & Upload Guidelines (SOP)

This document provides a comprehensive guide for data uploaders to sanitize, format, and validate Network Access Point (NAP / Fixed Wired Broadband) Excel files before uploading them to the system. Following these guidelines ensures smooth processing, accurate geographic mapping, and prevents batch processing failures.

> [!IMPORTANT]
> **Strict Pre-Processing Rule**: All files **must** be sanitized and realigned based on the **latest official Excel template**. The system will only accept post-processed files. Any unformatted, misaligned, or missing required fields will cause records to be skipped or the upload to fail.

---

## 1. Summary Checklist for Uploaders

Before uploading any NAP file, verify that:
- [ ] All columns align exactly with the latest official Excel template (`Provider`, `Barangay`, `Municipality`, `Province`, `Region`, `Lat`, `Lng`).
- [ ] All 7 fields are 100% populated with valid, non-empty data.
- [ ] `Provider` matches one of the registered **Wired Service Providers** in the QoS database (e.g., PLDT, GLOBE, CONVERGE).
- [ ] Coordinate columns (`Lat`, `Lng`) contain **clean decimal numbers only** (WGS84 format, no degree symbols, compass letters, or commas).
- [ ] Coordinates fall strictly within valid Philippine geographic boundaries (Latitude: 4.0° to 21.5° N, Longitude: 116.0° to 127.0° E).
- [ ] Latitude and Longitude values are **not inverted/swapped** (`Lat` in Column F, `Lng` in Column G).
- [ ] Location names (`Barangay`, `Municipality`, `Province`, `Region`) use standard administrative spellings without placeholder characters.
- [ ] Empty optional cells or remarks are left completely **BLANK** (no `"---"`, `"N/A"`, or `"null"` placeholders).

---

## 2. Required Fields (Mandatory Columns)

All 7 columns in the NAP template are **Mandatory Required Fields**. If any cell is blank, invalid, or out of range, the record will be rejected:

| Header Name | Column | Type | Description | Action Required if Missing / Invalid |
| :--- | :--- | :--- | :--- | :--- |
| `Provider` | A | Dropdown / Text | Name of the wired telecommunications provider | Select valid wired ISP from dropdown; do not use custom names |
| `Barangay` | B | Text | Barangay where the NAP is situated | Populate with official barangay name |
| `Municipality` | C | Text | City or Municipality of the installation | Populate with official city/municipality name |
| `Province` | D | Text | Province of the installation | Populate with official province name |
| `Region` | E | Text | Administrative Region (e.g., `Region X`) | Populate with official region name |
| `Lat` | F | Decimal | WGS84 Latitude coordinate | Ensure valid decimal format; delete row if coordinates unavailable |
| `Lng` | G | Decimal | WGS84 Longitude coordinate | Ensure valid decimal format; delete row if coordinates unavailable |

> [!CAUTION]
> If a row is missing valid geographic coordinates or location details and valid data cannot be verified, **delete the entire row** from the spreadsheet before uploading.

---

## 3. Geographic Coordinates Standardization (`Lat`, `Lng`)

Accurate geographic coordinates are essential for mapping fixed broadband connectivity and computing gap analyses.

### Mandatory Format: WGS84 Decimal Degrees
- **Latitude Example**: `8.48222` (Valid range for Philippines: `4.0` to `21.5`)
- **Longitude Example**: `124.64722` (Valid range for Philippines: `116.0` to `127.0`)

### ⚠️ Common Coordinate Errors to Avoid:
1. **Inverted Coordinates (Swapped Lat/Lng)**:
   - Entering Longitude (~`124.x`) into the `Lat` column will cause an immediate validation failure because latitude cannot exceed `90.0`.
2. **DMS (Degrees, Minutes, Seconds) Format**:
   - Coordinates such as `8°28'56"N` or `124°38'50"E` are **unsupported**. Convert them to standard decimal degrees prior to upload.
3. **Leading / Trailing Whitespace**:
   - Text spaces (e.g., `" 8.48222 "`) can break parsing. Trim all cell values.

---

## 4. Numeric & Decimal Value Sanitization

Coordinate fields (`Lat`, `Lng`) must contain **clean numeric data only**:

### Formatting Rules:
- **No Extra Spaces**: Remove leading, trailing, or embedded spaces (e.g., `" 124.65 "` &rarr; `124.65`).
- **Use Period (`.`) for Decimals**: Replace European decimal commas with periods (e.g., `8,4822` &rarr; `8.4822`).
- **No Units or Directional Letters**: Remove letters such as `N`, `S`, `E`, `W`, `deg`, or degree symbols (e.g., `8.4822N` &rarr; `8.4822`).

---

## 5. Service Provider Validation

The `Provider` column must strictly match a registered **Wired Service Provider** in the QoS portal database.

| Column | Allowed Values / Standard | Action Required |
| :--- | :--- | :--- |
| `Provider` | Official Registered Wired Telco (e.g., `PLDT`, `GLOBE`, `CONVERGE`, `DITO`) | Select directly from the template dropdown list. If all records fail to match an active wired provider, the entire upload is aborted. |

> [!WARNING]
> **Wired vs. Wireless Discrepancy**: NAP uploads only accept **Wired** service providers. If a provider in the file is categorized strictly as wireless or does not exist in the database, the records for that provider will be skipped.

---

## 6. Blank Values vs. Dummy Placeholders

For non-mandatory or address clarification cells:
- Leave the cell completely **BLANK (EMPTY)**.
- **DO NOT** type placeholder characters such as `"---"`, `"-"`, `"N/A"`, `"null"`, `"NONE"`, or `"0"` (unless zero is an actual valid coordinate, which is not applicable in the Philippines).

---

## 7. Duplicate Handling & Address Resolution

| Scenario | Definition | System Action |
| :--- | :--- | :--- |
| **Identical NAP Record** | A NAP with the same Provider, Address (`Barangay, Municipality, Province, Region`), and geographic coordinate already exists in the system or earlier in the file. | **Automatically Skipped**: The duplicate record is bypassed safely to prevent data inflation. |
| **New Point / Updated Facility** | A NAP with unique coordinates or a new address for that provider. | **Inserted**: New NAP facility is added to the spatial database. |
| **Scheduled Processing** | Upon successful batch insertion of NAP records. | **Automated Assessment**: The system triggers connectivity gap assessments in the background. |

---

## 8. Post-Upload Support

If your file encounters processing errors even after following this guide:
1. Check the upload status dialog in the **NAP** tab of the QoS portal.
2. Note any reported row numbers, missing provider warnings, or out-of-bounds coordinate notices.
3. Review the template alignment and field types for those specific rows.
4. Contact the system administrator with the file name and error message for further assistance.

---

![NTC-QoS Logo](C:\Users\almau\.gemini\antigravity\brain\43c5eb33-c84d-41b3-bfb2-16faf625415e\ntc_qos_logo.png)

**National Telecommunications Commission — Quality of Service (QoS) Portal**  
*Official NAP Data Pre-processing SOP Document*
