# Cellular Coverage & Signal Strength Data Pre-processing & Upload Guidelines (SOP)

This document provides a comprehensive operational guide for data uploaders to sanitize, format, and validate Cellular Coverage and Signal Strength Excel files before uploading them to the QoS portal. Following these guidelines ensures precise geospatial propagation heatmaps, accurate coverage boundary generation, and prevents batch processing failures.

> [!IMPORTANT]
> **Strict Pre-Processing Rule**: All files **must** be sanitized and realigned based on the **latest official Excel template**. The system will only accept post-processed files. Any unformatted, misaligned, or missing required fields will cause rows to fail or the batch to abort.

---

## 1. Summary Checklist for Uploaders

Before uploading any Cellular Coverage file, verify that:
- [ ] Columns match the official template exactly: `ServiceProvider`, `Network`, `Lat`, `Lng`, `Strength`, `Address`.
- [ ] Data rows begin on **Row 2** immediately below the single header row.
- [ ] Required fields (`ServiceProvider`, `Network`, `Lat`, `Lng`, `Strength`) are 100% populated.
- [ ] `ServiceProvider` matches one of the registered **Wireless Service Providers** in the system.
- [ ] `Network` matches an exact pre-configured network name in the QoS portal (e.g., `DITO 4G 2600`, `GLOBE 4G 1800`, `SMART 5G 3500`).
- [ ] `Strength` is a **clean numeric value** with the negative sign preserved (e.g., `-85.5`, not `"-85.5 dBm"`).
- [ ] Coordinate fields (`Lat`, `Lng`) contain clean WGS84 decimals without degree symbols or directional letters (`N`, `E`).
- [ ] Latitude and Longitude values are not swapped (`Lat` in Col C, `Lng` in Col D).
- [ ] Optional field `Address` is formatted cleanly; empty cells are left **BLANK** (no `"---"`, `"N/A"`, or `"null"`).

---

## 2. Required Fields (Mandatory Columns)

The following 5 columns are **Mandatory Required Fields**. If any cell is blank or contains non-numeric/unmapped data, that row cannot be imported:

| Header Name | Column | Type | Description | Action Required if Missing / Invalid |
| :--- | :--- | :--- | :--- | :--- |
| `ServiceProvider` | A | Dropdown | Registered wireless telecommunications provider | Select from approved dropdown list |
| `Network` | B | Dropdown | Network technology and band configuration | Select exact network name pre-configured in portal |
| `Lat` | C | Decimal | WGS84 Latitude coordinate | Ensure valid decimal format; delete row if unavailable |
| `Lng` | D | Decimal | WGS84 Longitude coordinate | Ensure valid decimal format; delete row if unavailable |
| `Strength` | E | Decimal | Measured RF signal strength (e.g., RSRP / RSSI in dBm) | Enter clean decimal value preserving negative sign; delete row if empty |
| `Address` | F | Text | Optional location or descriptive address | Optional; leave blank if unavailable |

> [!CAUTION]
> If a row is missing `ServiceProvider`, `Network`, `Lat`, `Lng`, or `Strength`, **delete the entire row** from the spreadsheet before uploading.

---

## 3. Signal Strength (`Strength`) Sanitization

The `Strength` column represents RF field strength measurements (typically measured as RSRP or RSSI in dBm).

### Formatting Rules:
- **Preserve the Negative Sign (`-`)**: Mobile RF signal measurements are almost always negative (e.g., `-70` to `-120 dBm`). Do not omit the minus symbol.
- **No Units**: Strip all text units such as `dBm`, `dB`, or `m` (e.g., `-88.5 dBm` &rarr; `-88.5`).
- **No Commas or Spaces**: Replace decimal commas with periods (e.g., `-92,4` &rarr; `-92.4`).
- **Valid Signal Range**: Values typically range from `-140.0` (extremely weak) to `-44.0` (extremely strong).

---

## 4. Geographic Coordinates Standardization (`Lat`, `Lng`)

Accurate geographic coordinates are required to plot signal strength onto high-resolution raster heatmaps.

### Mandatory Format: WGS84 Decimal Degrees
- **Latitude Example**: `8.48222` (Valid range for Philippines: `4.0` to `21.5`)
- **Longitude Example**: `124.64722` (Valid range for Philippines: `116.0` to `127.0`)

### Rules:
- **No Swapping**: Never place longitude in Column C or latitude in Column D.
- **No DMS Format**: Convert values like `14°35'45"N` to decimal degrees before importing.
- **Pure Numbers Only**: Strip letters like `N`, `E`, and degree symbols (`°`).

---

## 5. Dropdown Columns Validation

Dropdown values must match portal records with exact spelling and casing:

| Column | Allowed Values / Standard | Action Required |
| :--- | :--- | :--- |
| `ServiceProvider` | Registered Wireless Operators (e.g., `DITO TELECOMMUNITY CORP.`, `GLOBE COMMUNICATIONS INC.`, `SMART COMMUNICATIONS INC.`) | Select from template dropdown list. Unrecognized operators will cause rows to fail. |
| `Network` | Registered Network Names (e.g., `DITO 4G 2600`, `GLOBE 4G 1800`, `SMART 5G 3500`, etc.) | Must match an existing Network configured in the database under that Provider. If a new network standard is introduced, request admin setup prior to upload. |

> [!WARNING]
> **Network-Provider Alignment**: The selected `Network` must belong to the specified `ServiceProvider`. If a row lists `GLOBE` with a `DITO 4G 2600` network, the system will not be able to link the coverage point.

---

## 6. Blank Values vs. Dummy Placeholders

For the optional `Address` column:
- Leave the cell completely **BLANK (EMPTY)**.
- **DO NOT** type placeholder characters such as `"---"`, `"-"`, `"N/A"`, `"null"`, `"NONE"`, or `"0"`.

---

## 7. Spatial Deduplication & Checkpoint Resume

| Feature | Behavior |
| :--- | :--- |
| **Spatial Deduplication** | The system creates spatial indexes based on `(NetworkId, BandId, Location)`. Repeated identical coordinates for the same network are deduplicated to optimize map rendering performance. |
| **Checkpoint Resume** | In the event of large multi-megabyte datasets, the system saves progress checkpoints periodically. Interrupted uploads can resume from the last saved row automatically. |

---

## 8. Post-Upload Support

If your file encounters processing errors:
1. Check the upload status dialog in the **Cellular Coverage** tab.
2. Confirm that the selected `Network` names exist in the system directory.
3. Review any reported rows for malformed coordinates or non-numeric strength entries.
4. Contact the system administrator with the error message for further assistance.

---

![NTC-QoS Logo](C:\Users\almau\.gemini\antigravity\brain\43c5eb33-c84d-41b3-bfb2-16faf625415e\ntc_qos_logo.png)

**National Telecommunications Commission — Quality of Service (QoS) Portal**  
*Official Cellular Coverage Data Pre-processing SOP Document*
