# Wireless Validation Data Pre-processing & Upload Guidelines (SOP)

This document provides a comprehensive operational guide for data uploaders to sanitize, format, and validate Wireless Validation Excel files before uploading them to the QoS portal. Wireless Validation files capture field survey ground-truth data regarding which mobile telecom operators and frequency bands provide actual coverage across each barangay.

> [!IMPORTANT]
> **Municipality-Specific Template**: Wireless Validation templates **must** be downloaded directly from the portal for the specific target Municipality. The portal pre-populates the template with official barangays and the critical **Location Header in Cell A1**. Never modify Cell A1 or alter the 4-row header structure.

---

## 1. Summary Checklist for Uploaders

Before uploading any Wireless Validation file, verify that:
- [ ] Template was downloaded from the portal with the target **Province** and **Municipality** selected.
- [ ] **Cell A1** contains the exact location header string: `<Municipality>, <Province>, <Region>`.
- [ ] Multi-row headers (Rows 1 to 4) are preserved intact; **survey data begins strictly on Row 5**.
- [ ] Column A (`Provider`) contains approved wireless operators (`GLOBE COMMUNICATIONS INC.`, `SMART COMMUNICATIONS INC.`, `DITO TELECOMMUNITY CORP.`).
- [ ] Column B (`Barangay`) matches official PSGC barangay names pre-filled by the portal template.
- [ ] Frequency band columns contain only valid affirmative (`TRUE`, `1`, `YES`) or negative (`FALSE`, `0`, `NO`, or blank) entries.
- [ ] No custom text descriptions (e.g., `"Good"`, `"Poor"`, `"May signal"`, `"N/A"`) are entered in band columns.
- [ ] Empty/untested bands are left completely **BLANK** or marked as `FALSE`.

---

## 2. Template Structure & Header Layout

The Wireless Validation spreadsheet utilizes a pre-configured 4-row hierarchical header:

| Row / Location | Item | Details |
| :--- | :--- | :--- |
| **Cell A1** | **Location Header** | **Critical Identifier**: `<Municipality>, <Province>, <Region>` (e.g., `Tagoloan, Misamis Oriental, Region X`). Used by the backend engine to match the LGU database. |
| **Row 2** | **Generation** | Cellular generations: `2G`, `3G`, `4G`, `5G` spanning across band groups |
| **Row 3** | **Technology** | Transmission standards: `GSM`, `WCDMA`, `FDD-LTE`, `TDD-LTE`, `NR` |
| **Row 4** | **Columns & Bands** | Col A: `Provider`, Col B: `Barangay`, Cols C+: `B8-900`, `B3-1800`, ..., `B78-3500` |
| **Row 5+** | **Data Records** | **Survey rows begin on Row 5** (Barangays pre-multiplied across each Provider) |

> [!CAUTION]
> **Do not edit Cell A1**: If Cell A1 is emptied, deleted, or misaligned, the backend parser cannot resolve the municipality, causing the entire file upload to be rejected immediately.

---

## 3. How to Obtain the Official Template

Because each municipality has a unique set of official barangays, templates are generated dynamically:
1. Log into the QoS Portal and navigate to the **Files** / **Upload** section.
2. Select the **Wireless Validation** tab.
3. Select your **Province** from the dropdown menu.
4. Select your **Municipality** from the dropdown menu.
5. Click **Download Wireless Validation template file**.
6. The downloaded file (e.g., `wireless_validation_template-Tagoloan.xlsx`) will have Cell A1, all barangays, and telco rows pre-populated and color-coded.

---

## 4. Required Fields & Validation Columns

| Header / Cell | Type | Description | Rule / Action |
| :--- | :--- | :--- | :--- |
| **Cell A1** | Formatted Text | LGU Geographic identifier | Must match: `<Municipality>, <Province>, <Region>` |
| `Provider` (Col A) | Text | Wireless service provider | Must match registered wireless telco names |
| `Barangay` (Col B) | Text | Official barangay name | Must match official LGU barangay records |
| **Band Columns** (Cols C+) | Boolean | Verified signal reception in that barangay | Set to `TRUE` if signal/coverage is validated; `FALSE` or blank if absent |

---

## 5. Valid Values for Frequency Band Columns

The validation engine processes survey results using standard boolean logic. Enter only accepted values:

| Intended Status | Accepted Values | Unacceptable Values (Will Cause Errors) |
| :--- | :--- | :--- |
| **Verified Coverage Present** | `TRUE`, `1`, `YES`, `Y` | `"Available"`, `"Good"`, `"May Signal"`, `"Active"`, `"Checked"` |
| **No Coverage / Inactive** | `FALSE`, `0`, `NO`, `N`, or leave **BLANK** | `"None"`, `"No Signal"`, `"N/A"`, `"---"`, `"null"` |

> [!WARNING]
> Do not use arbitrary descriptive text in the band cells. Any value other than recognized boolean representations will either evaluate to `FALSE` or fail validation.

---

## 6. Barangay Name Integrity

- **Do Not Rename Pre-filled Barangays**: The system cross-references each row's barangay name against official database records.
- **Handling Split / Historic Barangays**: Use the standardized official name found in the downloaded template.
- If a barangay row is modified with unknown characters or misspellings, the system will log:  
  `"Skipping record without resolved barangay: Provider=..., Barangay='...'"`

---

## 7. Duplicate Detection & Audit Logging

| Mechanism | Description |
| :--- | :--- |
| **Automated Deduplication** | The system computes a hash key based on `(ServiceProviderId, BarangayId, BandId, TechnologyId, Address)`. Repeated submissions for the same barangay, provider, and band are updated seamlessly without creating duplicates. |
| **Audit Compliance** | All validated records store the timestamp and authenticated user ID of the regional officer who submitted the validation. |

---

## 8. Post-Upload Support

If your file encounters processing errors:
1. Verify that **Cell A1** has not been modified or cleared.
2. Confirm that data starts on **Row 5** and header rows 1 to 4 have not been modified.
3. Review whether non-standard characters were pasted into the frequency band columns.
4. Contact the system administrator with the error log and municipality name for prompt resolution.

---

![NTC-QoS Logo](C:\Users\almau\.gemini\antigravity\brain\43c5eb33-c84d-41b3-bfb2-16faf625415e\ntc_qos_logo.png)

**National Telecommunications Commission — Quality of Service (QoS) Portal**  
*Official Wireless Validation Data Pre-processing SOP Document*
