# Cell Site & Transceiver Data Pre-processing & Upload Guidelines (SOP)

This document provides a comprehensive operational guide for data uploaders to sanitize, format, and validate Cell Site and Base Transceiver Station (BTS) Excel files before uploading them to the QoS portal. Adhering to these specifications guarantees proper spatial mapping, RF propagation modeling, and prevents processing failures.

> [!IMPORTANT]
> **Multi-Row Header Structure**: The Cell Site template utilizes a specialized **3-row composite header** (Row 1: Generation, Row 2: Technology, Row 3: Band / Property). **Data rows begin strictly on Row 4**. Never delete, merge, reorder, or alter the header rows.

---

## 1. Summary Checklist for Uploaders

Before uploading any Cell Site file, verify that:
- [ ] Template maintains the exact 3-row header hierarchy; data rows begin on **Row 4**.
- [ ] Required location and provider fields (`Provider`, `Municipality`, `Province`, `Region`, `Lat`, `Long`) are fully populated.
- [ ] **Active Technology Rule**: At least ONE band column is set to `TRUE` (or contains valid center frequency `CF: <val>, BW: <val>`). Rows where all band columns are `FALSE` or empty cannot be used for coverage analysis.
- [ ] `Provider` matches one of the registered **Wireless Service Providers** (e.g., `DITO TELECOMMUNITY CORP.`, `GLOBE COMMUNICATIONS INC.`, `SMART COMMUNICATIONS INC.`).
- [ ] Coordinates (`Lat`, `Long`) contain clean decimal numbers in WGS84 format without degree symbols or text letters.
- [ ] `Tower Type` and `AntennaType` use exact approved dropdown values.
- [ ] `Antenna Height (m)` is a pure positive numeric value (meters, no `"m"` unit).
- [ ] `Azimuth (°)` is a valid heading between `0` and `360` degrees.
- [ ] `Beam Tilt (°)` contains numeric tilt angles only.
- [ ] Empty optional cells are left completely **BLANK** (no `"---"`, `"N/A"`, or `"null"` placeholders).

---

## 2. Header Structure & Data Start Row

The template is organized as a 3-tier hierarchy:

| Row Number | Level | Description |
| :--- | :--- | :--- |
| **Row 1** | **Generation** | Defines cellular generations: `2G`, `3G`, `4G`, `5G`, plus antenna parameters |
| **Row 2** | **Technology** | Defines air interface standards: `GSM`, `WCDMA`, `FDD-LTE`, `TDD-LTE`, `NR` |
| **Row 3** | **Band / Column** | Core site info, specific band codes (`B8-900`, `B3-1800`, `B28-700`, `B78-3500`), and RF properties |
| **Row 4+** | **Data Rows** | **First actual site record begins on Row 4** |

> [!CAUTION]
> If data is mistakenly pasted starting on Row 1, 2, or 3, the entire file parser will fail immediately due to header mismatch.

---

## 3. Required Fields & Mandatory Core Columns

The following columns are **Mandatory Core Fields**. Rows missing any of these items will be skipped during processing:

| Header Name (Row 3) | Type | Description | Action Required if Missing |
| :--- | :--- | :--- | :--- |
| `Provider` | Dropdown | Registered wireless telecommunications provider | Select from approved wireless telco dropdown |
| `Municipality` | Text | City / Municipality of cell tower site | Provide official LGU municipality name |
| `Province` | Text | Province of cell tower site | Provide official province name |
| `Region` | Text | Administrative Region (e.g., `Region X`) | Provide official region designation |
| `Lat` | Decimal | WGS84 Latitude coordinate | Ensure valid decimal format; delete row if unavailable |
| `Long` | Decimal | WGS84 Longitude coordinate | Ensure valid decimal format; delete row if unavailable |
| **Band Columns** | Boolean / Text | Operating frequency bands (Columns I to AD) | **At least ONE band must be set to `TRUE`** |

---

## 4. Critical Active Technology Rule (Coverage Analysis)

Every cellular tower sector represents RF transceivers transmitting on specific licensed spectrum bands.

### The Rule:
**At least ONE band column per row must be set to `TRUE` (or valid CF/BW configuration).**

| Data State in Row | System Processing Behavior | Coverage Impact |
| :--- | :--- | :--- |
| **At least one band is `TRUE`** (e.g., `B28-700 = TRUE`) | • Creates `Cell` (Tower site)<br>• Creates `CellSector` (Antenna structure)<br>• Creates `Cellular` (Active RF transceivers)<br>• Auto-generates `Network` & `Sector` | ✅ **Fully Functional**: Tower appears on coverage maps, wireless assessments, and connectivity analytics. |
| **ALL bands are `FALSE` or empty** | • Creates `Cell` (Tower site)<br>• Creates `CellSector` (Antenna structure)<br>❌ **Does NOT create Cellular records**<br>❌ **Does NOT create Network or Sector records** | ⚠️ **Degraded State**: Tower site is saved in inventory, but **CANNOT** be analyzed, visualized on heatmap layers, or used for signal coverage calculations. |

> [!WARNING]
> If your upload logs display: `"Pre-validation Warning: Found X records with ALL technology/band columns set to FALSE or empty"`, those rows will not produce coverage analysis data. Verify the spreadsheet and set active bands to `TRUE` before uploading.

---

## 5. Dropdown Columns Validation

Dropdown fields must strictly conform to system-recognized values:

| Column Header | Allowed Values | Remarks |
| :--- | :--- | :--- |
| `Provider` | `DITO TELECOMMUNITY CORP.`, `GLOBE COMMUNICATIONS INC.`, `SMART COMMUNICATIONS INC.` | Select strictly from the dropdown list. |
| `Tower Type` | `GuyedTower`, `Monopole`, `Rooftop`, `SelfSupporting`, `Others`, `Unknown` | Select physical structural tower type. |
| `AntennaType` | `OmniDirectional`, `Directional`, `Others`, `Unknown` | Select antenna radiation pattern type. |
| **Band Columns** (Cols 9 to 30) | `TRUE`, `FALSE`, or blank | • Set to `TRUE` if transmitter operates on that band.<br>• Set to `FALSE` or leave blank if not equipped. |

---

## 6. Numeric & RF Parameters Sanitization

All numerical and RF engineering parameters must contain clean values without formatting suffixes:

| Field | Example Valid Value | Invalid Entries (Will Fail) | Rule |
| :--- | :--- | :--- | :--- |
| `Lat` | `8.48222` | `8°28'56"N`, `8,48222`, `N 8.48` | Clean decimal latitude only |
| `Long` | `124.64722` | `124°38'50"E`, `124,64722` | Clean decimal longitude only |
| `Antenna Height (m)` | `35` or `42.5` | `35m`, `35 meters`, `~35` | Numeric height in meters only |
| `Azimuth (°)` | `120` | `120 deg`, `120°`, `SE` | Directional angle between 0 and 360 |
| `Beam Tilt (°)` | `2` or `4.5` | `2 degrees`, `2 deg` | Mechanical/electrical tilt angle |

---

## 7. Blank Values vs. Dummy Placeholders

For optional parameters (e.g., `Zone`, `Barangay` if unincorporated, or inactive band columns):
- Leave the cell completely **BLANK (EMPTY)**.
- **DO NOT** type placeholder characters such as `"---"`, `"-"`, `"N/A"`, `"null"`, `"NONE"`, or `"#REF!"`.

---

## 8. Post-Upload Support

If your file encounters processing errors:
1. Review the progress logs in the **Cell** tab of the portal.
2. Note the batch number and reported record index.
3. Check whether the failure stems from missing coordinates, unrecognized provider names, or invalid non-numeric antenna heights.
4. Contact the system administrator with the file name and log excerpt for assistance.

---

![NTC-QoS Logo](C:\Users\almau\.gemini\antigravity\brain\43c5eb33-c84d-41b3-bfb2-16faf625415e\ntc_qos_logo.png)

**National Telecommunications Commission — Quality of Service (QoS) Portal**  
*Official Cell Site Data Pre-processing SOP Document*
