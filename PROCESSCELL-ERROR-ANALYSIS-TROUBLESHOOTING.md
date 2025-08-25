# ?? ProcessCellJob Error Analysis & Troubleshooting Guide

## ?? Overview

Based on analysis of the ProcessCellJob code, here are the most common errors that can occur during cell processing and their root causes:

## ?? Critical Error Categories

### **1. Missing Database Dependencies**

#### **Error: Service Provider Not Found**
```log
CRITICAL ERROR: Wireless Service Provider not found in pre-loaded cache: {Provider}. Terminating process.
```

**Root Cause:**
- Provider mentioned in Excel doesn't exist in database
- Provider exists but isn't marked as 'Wireless' service type
- Case sensitivity mismatch in provider name

**Solution:**
```sql
-- Check existing service providers
SELECT sp.*, p.Name, s.Type 
FROM ServiceProviders sp
JOIN Providers p ON sp.ProviderId = p.Id
JOIN Services s ON sp.ServiceId = s.Id
WHERE s.Type = 'Wireless';

-- Add missing provider if needed
INSERT INTO ServiceProviders (Id, ServiceId, ProviderId)
SELECT NEWID(), s.Id, p.Id
FROM Services s, Providers p
WHERE s.Type = 'Wireless' AND p.Name = 'YourProviderName';
```

#### **Error: Technology Not Found**
```log
CRITICAL ERROR: Technology '{Technology}' not found in database. 
Processing terminated at Excel row {RowNumber}. 
Available technologies: {AvailableTechnologies}
```

**Root Cause:**
- Technology from Excel column (GSM, WCDMA, FDDLTE, TDDLTE, NR) doesn't exist
- Naming convention mismatch between Excel and database

**Solution:**
```sql
-- Check existing technologies
SELECT * FROM Technologies ORDER BY Standard;

-- Add missing technologies
INSERT INTO Technologies (Id, Standard, GenerationId) VALUES
(NEWID(), 'GSM', @2GId),
(NEWID(), 'EDGE', @2GId),
(NEWID(), 'WCDMA', @3GId),
(NEWID(), 'HSPAP', @3GId),
(NEWID(), 'FDDLTE', @4GId),
(NEWID(), 'TDDLTE', @4GId),
(NEWID(), 'LTE', @4GId),
(NEWID(), 'NR', @5GId);
```

#### **Error: Band Not Found**
```log
CRITICAL ERROR: Band '{Band}' not found in database. 
Processing terminated at Excel row {RowNumber}. 
Available bands: {AvailableBands}
```

**Root Cause:**
- Frequency bands (Band 28, Band 5, Band 8, etc.) missing from database
- Band naming inconsistency

**Solution:**
```sql
-- Check existing bands
SELECT * FROM Bands ORDER BY Name;

-- Add missing bands
INSERT INTO Bands (Id, Name, FrequencyRange) VALUES
(NEWID(), 'Band 28', '700 MHz'),
(NEWID(), 'Band 5', '850 MHz'),
(NEWID(), 'Band 8', '900 MHz'),
(NEWID(), 'Band 3', '1800 MHz'),
(NEWID(), 'Band 1', '2100 MHz'),
(NEWID(), 'Band 40', '2300 MHz'),
(NEWID(), 'Band 41', '2500 MHz'),
(NEWID(), 'Band 38', '2600 MHz'),
(NEWID(), 'Band 78', '3500 MHz');
```

#### **Error: SystemType Not Found**
```log
CRITICAL ERROR: SystemType not found for Technology: {TechnologyInfo}. 
Cannot create network without existing SystemType.
```

**Root Cause:**
- Missing SystemType entries for technologies
- SystemTypes are required to create Networks

**Solution:**
```sql
-- Check existing system types
SELECT st.*, t.Standard FROM SystemTypes st
JOIN Technologies t ON st.TechnologyId = t.Id;

-- Add missing system types
INSERT INTO SystemTypes (Id, Name, TechnologyId)
SELECT NEWID(), t.Standard, t.Id
FROM Technologies t
WHERE NOT EXISTS (
    SELECT 1 FROM SystemTypes st WHERE st.TechnologyId = t.Id
);
```

### **2. Data Format Errors**

#### **Error: Coordinate Validation Issues**
```log
WARNING: Detected swapped coordinates in Excel row {RowNumber}. 
Original: Lat={OriginalLat}, Lon={OriginalLon}. 
Corrected: Lat={CorrectedLat}, Lon={CorrectedLon}
```

**Root Cause:**
- Latitude and Longitude columns swapped in Excel
- Coordinates outside valid ranges

**Solution:**
- Verify Excel column headers: "Lat" and "Long"
- Ensure latitude: -90 to 90, longitude: -180 to 180
- Check coordinate order in Excel data

#### **Error: Azimuth/BeamTilt Mismatch**
```log
ERROR: The number of Azimuth values ('{AzimuthCount}') does not match 
the number of BeamTilt values ('{BeamTiltCount}') for record in Excel row {RowNumber}
```

**Root Cause:**
- Unequal comma-separated values in Azimuth and BeamTilt columns
- Missing or extra values in sector configuration

**Solution:**
```excel
// Correct format:
Azimuth: "0,120,240"
BeamTilt: "3,3,3"

// Incorrect format:
Azimuth: "0,120,240"
BeamTilt: "3,3"  // Missing third value
```

### **3. Municipality/Location Errors**

#### **Error: Municipality Not Found**
```log
WARNING: Skipping cellular validation for Cell {CellId} because no municipality 
could be determined for location: {Location}
```

**Root Cause:**
- Missing municipality boundary data
- Cell location outside all municipality boundaries
- Invalid coordinate data

**Solution:**
```sql
-- Check municipality boundaries
SELECT Id, Name, 
       CASE WHEN Boundaries IS NULL THEN 'Missing' ELSE 'Present' END as BoundaryStatus
FROM Municipalities 
ORDER BY Name;

-- Update missing boundaries
UPDATE Municipalities 
SET Boundaries = ST_GeomFromText('POLYGON(...)', 4326)
WHERE Boundaries IS NULL AND Name = 'YourMunicipalityName';
```

### **4. Network Creation Failures**

#### **Error: Network Dependencies Missing**
```log
WARNING: Cellular entities skipped due to missing network dependencies: {SkippedDependencyCount}
```

**Root Cause:**
- Missing SystemType for technology
- Network creation fails due to foreign key constraints

**Verification:**
```sql
-- Check network creation dependencies
SELECT 
    t.Standard as Technology,
    COUNT(st.Id) as SystemTypeCount,
    COUNT(DISTINCT sp.Id) as ServiceProviderCount
FROM Technologies t
LEFT JOIN SystemTypes st ON t.Id = st.TechnologyId
LEFT JOIN ServiceProviders sp ON sp.Service.Type = 'Wireless'
GROUP BY t.Standard
HAVING COUNT(st.Id) = 0; -- Technologies without SystemTypes
```

### **5. Excel Format Issues**

#### **Error: Technology/Band Column Parsing**
```log
WARNING: Could not parse band/technology value '{Value}' for {Technology} {Band} 
in row {RowNumber}. Defaulting to false.
```

**Root Cause:**
- Invalid format in technology/band columns
- Unexpected values in Boolean or frequency columns

**Valid Formats:**
```excel
// Boolean values
TRUE, FALSE, 1, 0

// Frequency format
CF: 2140.5, BW: 20

// Empty (defaults to FALSE)
```

#### **Error: Property Mapping Issues**
```log
ERROR: Could not parse '{TrimmedAzimuth}' as a decimal for record in Excel row {RowNumber}
```

**Root Cause:**
- Non-numeric values in numeric columns
- Incorrect decimal separators
- Extra spaces or invalid characters

## ?? Debugging Steps

### **1. Enable Detailed Logging**
```csharp
// Add to appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "NTC.QoS.Infrastructure.BackgroundTasks.ProcessCellJob": "Debug"
    }
  }
}
```

### **2. Pre-Processing Validation**
```sql
-- Run before processing to check dependencies
SELECT 'Service Providers' as EntityType, COUNT(*) as Count 
FROM ServiceProviders sp 
JOIN Services s ON sp.ServiceId = s.Id 
WHERE s.Type = 'Wireless'

UNION ALL

SELECT 'Technologies', COUNT(*) FROM Technologies

UNION ALL

SELECT 'Bands', COUNT(*) FROM Bands

UNION ALL

SELECT 'SystemTypes', COUNT(*) FROM SystemTypes

UNION ALL

SELECT 'Municipalities with Boundaries', COUNT(*) 
FROM Municipalities WHERE Boundaries IS NOT NULL;
```

### **3. Excel Data Validation**
```excel
// Check for common issues:
1. Provider names match database exactly
2. Coordinate ranges: Lat (-90,90), Long (-180,180)
3. Technology columns have valid values
4. Azimuth/BeamTilt have equal comma-separated count
5. Numeric columns contain only numbers
```

## ?? Quick Fixes

### **For Missing Dependencies:**
```sql
-- Quick setup script for testing
-- Add basic technologies
INSERT INTO Technologies (Id, Standard, GenerationId) 
SELECT NEWID(), 'LTE', g.Id FROM Generations g WHERE g.Name = '4G';

-- Add basic bands
INSERT INTO Bands (Id, Name) VALUES (NEWID(), 'Band 3');

-- Add system types
INSERT INTO SystemTypes (Id, Name, TechnologyId)
SELECT NEWID(), t.Standard, t.Id FROM Technologies t;
```

### **For Excel Issues:**
1. **Save as new Excel file** to clear formatting issues
2. **Check column headers** match expected names exactly
3. **Validate coordinates** are in decimal degrees format
4. **Remove extra spaces** from all data cells
5. **Use consistent decimal separators** (period, not comma)

## ?? Monitoring Recommendations

### **Key Metrics to Monitor:**
- Processing success rate
- Technology/Band combination coverage
- Municipality boundary completeness
- Network creation success rate
- Cell sector to cellular entity ratio

### **Alerts to Set:**
- Process termination due to missing dependencies
- High number of skipped records
- Municipality boundary misses
- Coordinate validation failures

This comprehensive analysis should help identify and resolve the most common errors in the ProcessCellJob workflow.