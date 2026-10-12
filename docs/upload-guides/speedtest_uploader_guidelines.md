# Speedtest Data Pre-processing & Upload Guidelines (SOP)

This document provides a comprehensive guide for data uploaders to sanitize, format, and validate Speedtest Excel files before uploading them to the system. Following these guidelines ensures smooth processing, accurate QoS analytics, and prevents file processing failures.

> [!IMPORTANT]
> **Strict Pre-Processing Rule**: All files **must** be sanitized and realigned based on the **latest official Excel template**. The system will only accept post-processed files. Any unformatted or misaligned files will fail during batch processing.

---

## 1. Summary Checklist for Uploaders

Before uploading any file, verify that:
- [ ] All columns align exactly with the latest official Excel template.
- [ ] Required fields (**colored headers**) are 100% populated with valid data.
- [ ] Timestamps reflect **Philippine Standard Time (PHT, UTC+8)** and are strictly formatted as `MM/DD/YYYY, time` (Month first).
- [ ] Numbers and decimal fields contain no spaces, commas, or text symbols.
- [ ] Dropdown and boolean fields (`signal_quality`, `operator`, `network_type`, `nbc`, `success`) use exact valid values.
- [ ] Longitude header is strictly named **`lon`** (do not rename to `lng` or `long`).
- [ ] Rows with unsupported network types (e.g., `Wi-Fi`, `Ethernet`) are removed.
- [ ] Empty optional fields are left completely **BLANK** (no `"---"`, `"N/A"`, or `"null"` placeholders).
- [ ] Missing readings/unserved locations are marked as `nbc = TRUE` or removed from the file.

---

## 2. Required Fields (Colored Headers)

The following 6 columns are **Mandatory Required Fields** (indicated by colored headers in the template). If any of these cells are blank or invalid, the file processing will **FAIL**:

| Header Name | Type | Description | Action Required if Missing |
| :--- | :--- | :--- | :--- |
| `timestamp` | DateTime | Test date and time in Philippine Standard Time (`MM/DD/YYYY, time`) | Remove row or fix timestamp |
| `upload` | Decimal | Upload speed in Mbps | Remove row or verify reading |
| `download` | Decimal | Download speed in Mbps | Remove row or verify reading |
| `operator` | Dropdown | Telecom service provider | Select valid operator from dropdown |
| `lat` | Decimal | Latitude coordinate (WGS84) | Remove row if missing |
| `lon` | Decimal | Longitude coordinate (WGS84) — *Note: named `lon`* | Remove row if missing |

> [!CAUTION]
> If a row is missing any of these required fields and valid data cannot be retrieved, **delete the entire row** from the spreadsheet before uploading.

---

## 3. Timestamp Standardization (`MM/DD/YYYY, time`)

### Timezone Requirement: Philippine Standard Time (PHT, UTC+8)
- Test times must reflect **local Philippine Time** as captured by test handsets or drive-test tools.
- **Do NOT manually convert to UTC**: The QoS processing engine automatically normalizes PHT to UTC on ingestion. Manual subtraction will cause timestamps to be shifted backwards by another 8 hours.

### Mandatory Date Format: Month First (`MM/DD/YYYY`)
The system accepts both 24-hour and 12-hour (AM/PM) time notations, provided the **month comes first**:
- **24-Hour Format**: `MM/DD/YYYY, HH:mm:ss` (e.g., `07/10/2025, 14:30:00` for July 10, 2025, at 2:30 PM)
- **12-Hour Format**: `MM/DD/YYYY, hh:mm:ss tt` (e.g., `10/06/2026, 04:12:00 PM` or `10/06/2026 04:12 PM`)

### ⚠️ Critical Warning on Date Ambiguity
Excel and system date parsers evaluate dates using the `MM/DD/YYYY` standard:
1. **Invalid Day in Month Position**: A date like `25/06/2025, 08:30` will trigger an immediate **parsing error** because month `25` does not exist.
2. **Silent Data Misinterpretation (High Risk)**:
   - If a test occurred on **July 10, 2025** and is entered as `10/07/2025`, the system will interpret and store it as **October 07, 2025**.
   - This corrupts historical reporting and QoS analytics.

### How to Fix Timestamps in Excel:
1. Highlight the `timestamp` column.
2. Ensure cell values follow `MM/DD/YYYY, HH:mm:ss` or `MM/DD/YYYY, hh:mm:ss AM/PM` format.
3. Review dates where Day $\le 12$ to guarantee that the Month comes first.

---

## 4. Numeric & Decimal Value Sanitization

Decimal and numeric fields (e.g., `upload`, `download`, `lat`, `lon`, `rssi`, `ping`, `jitter`) must contain **clean numeric data only**.

### Formatting Rules:
- **No Extra Spaces**: Remove leading, trailing, or embedded spaces (e.g., `" 12.34 "` &rarr; `12.34`).
- **Use Period (`.`) for Decimals**: Replace European decimal commas with periods (e.g., `1,25` &rarr; `1.25`).
- **No Units/Text Symbols**: Remove units such as `Mbps`, `ms`, `dBm`, or degree symbols (e.g., `15.5 Mbps` &rarr; `15.5`).
- **Column Header Distinction**: The longitude column must remain named **`lon`** (do not rename to `lng` or `long`).

---

## 5. Dropdown & Boolean Columns Validation

Dropdown fields must strictly match the pre-approved option list. Custom text or unmapped values will cause file processing to **FAIL**.

| Column | Allowed Values | Mapping / Action Required |
| :--- | :--- | :--- |
| `signal_quality` | `VeryWeak`, `Weak`, `Fair`, `Good`, `Excellent` | Re-select from the template dropdown list. Do not enter custom descriptions. |
| `operator` | Registered telecom operators (e.g., `SMART`, `GLOBE`, `DITO`) | Select strictly from the dropdown list. Ensure capitalization matches. |
| `network_type` | `NR`, `LTE`, `TDDLTE`, `FDDLTE`, `HSPAP` | Map raw values:<br>• **5G** &rarr; `NR`<br>• **4G** &rarr; `LTE`, `TDDLTE`, or `FDDLTE`<br>• **3G** &rarr; `HSPAP`<br>• **Wi-Fi / Ethernet** &rarr; ❌ **Remove entire row** |
| `nbc` | `TRUE`, `FALSE`, `NBC` *(or leave BLANK)* | • Set to `TRUE` or `NBC` if no broadband coverage is detected.<br>• Set to `FALSE` or leave `BLANK` for normal active tests. |
| `success` | `TRUE`, `FALSE` *(or leave BLANK)* | • Set to `TRUE` if test execution completed successfully.<br>• Set to `FALSE` if test failed or aborted. |

> [!WARNING]
> **Remove Non-Mobile Rows**: If `network_type` is `Wi-Fi`, `Ethernet`, or any non-cellular network, **delete the entire row** from the file.

---

## 6. Blank Values vs. Dummy Placeholders

For non-required columns without data:
- Leave the cell completely **BLANK (EMPTY)**.
- **DO NOT** type placeholder characters such as `"---"`, `"-"`, `"N/A"`, `"null"`, `"NONE"`, or `"0"` (unless zero is an actual measured value).

---

## 7. Handling Zero Readings vs. NULL (NBC Logic)

| Scenario | Definition | System Action |
| :--- | :--- | :--- |
| **Valid Zero Reading** | Test was successfully executed, but measured speed was `0.00 Mbps` upload and download. | **Include in file**: Set `upload = 0` and `download = 0`. |
| **No Reading / NULL / Unserved Area (NBC)** | Test was not performed, signal was unavailable, or reading returned `NULL`. | **Mark `nbc = TRUE` (Recommended)**: The system automatically records zero throughput and registers the point for No Broadband Coverage gap analysis.<br>*(Alternatively, remove row from file).* |

---

## 8. Post-Upload Support

If your file encounters processing errors even after following this guide:
1. Record the exact row and column number specified in the error message dialog.
2. Verify the template alignment and field types for that row.
3. Confirm that timestamps follow the `MM/DD/YYYY` month-first standard in Philippine Time.
4. Contact the system administrator with the error log for further assistance.

---

![NTC-QoS Logo](C:\Users\almau\.gemini\antigravity\brain\43c5eb33-c84d-41b3-bfb2-16faf625415e\ntc_qos_logo.png)

**National Telecommunications Commission — Quality of Service (QoS) Portal**  
*Official Speedtest Data Pre-processing SOP Document*
