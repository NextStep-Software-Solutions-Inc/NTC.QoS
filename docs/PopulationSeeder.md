# Population Data Seeder

## Overview

The PopulationSeeder is designed to import census population data from Excel files into your location entities (Regions, Provinces, and Municipalities). It follows the same **static utility pattern** as `LocationSeeder` - simple, straightforward, and automatically called during application startup.

**Uses ClosedXML.Excel** - the same library already used throughout your project for Excel processing.

## Automatic Seeding

Population seeding is **automatically handled during application startup** through the infrastructure dependency injection. The system will:

1. Look for `census_population_data.xlsx` in the root directory
2. Automatically seed population data if the file exists
3. Log helpful information if the file is not found
4. Continue with other seeders regardless

No manual intervention or command-line parameters are required.

## Excel File Format

Place your Excel file as `census_population_data.xlsx` in the root directory. The file should contain these sheets:

### "Regions" Sheet
| Column | Field Name | Description |
|--------|------------|-------------|
| A | RegionName | Name of the region (e.g., "NATIONAL CAPITAL REGION (NCR)") |
| B | TotalPopulation | Total population in the region |
| C | HouseholdPopulation | Population living in households |
| D | NumberOfHouseholds | Total number of households |

### "Provinces" Sheet
| Column | Field Name | Description |
|--------|------------|-------------|
| A | RegionName | Name of the region this province belongs to |
| B | ProvinceName | Name of the province |
| C | TotalPopulation | Total population in the province |
| D | HouseholdPopulation | Population living in households |
| E | NumberOfHouseholds | Total number of households |

### "Municipalities" Sheet  
| Column | Field Name | Description |
|--------|------------|-------------|
| A | RegionName | Name of the region this municipality belongs to |
| B | ProvinceName | Name of the province this municipality belongs to |
| C | MunicipalityName | Name of the municipality/city |
| D | TotalPopulation | Total population in the municipality |
| E | HouseholdPopulation | Population living in households |
| F | NumberOfHouseholds | Total number of households |

## File Setup

### 1. Default File Location (Recommended)
```
NTC.QoS-Backend/
??? census_population_data.xlsx    ? Place your Excel file here
??? admin1.geojson                 ? Your region GeoJSON files
??? admin2.geojson                 
??? admin3.geojson
??? admin4.geojson
??? [other project folders]
```

### 2. Automatic Detection
The system automatically checks for the file during startup:
- ? **File exists**: Population data is seeded automatically
- ?? **File missing**: Logs helpful guidance about file placement

## Integration with Application

### Current Implementation
Population seeding is integrated into the application startup pipeline in `InfrastructureDependencyInjection.cs`:

```csharp
// Automatic seeding during application startup
await LocationSeeder.SeedAllAsync(dbContext, logger);                    // Geographic data first
await PopulationSeeder.SeedPopulationDataIfExistsAsync(dbContext, logger); // Population data
await OrganizationSeeder.SeedAsync(dbContext, logger);                  // Continue with other seeders
```

### Available Methods

#### 1. Automatic Seeding (Default)
```csharp
// Handles file detection and logging automatically
await PopulationSeeder.SeedPopulationDataIfExistsAsync(context, logger);
```

#### 2. Manual Seeding (Custom File Path)
```csharp
// For custom file paths or manual seeding scenarios
await PopulationSeeder.SeedPopulationDataAsync(context, filePath, logger);
```

## Features

### Excel Multi-Sheet Support
- **Native Excel format**: Works directly with your .xlsx files using ClosedXML
- **Multiple sheets**: Processes Regions, Provinces, and Municipalities sheets
- **Flexible structure**: Can handle any combination of sheets
- **Consistent with project**: Uses the same Excel library as your other components

### Intelligent Matching
- **Fuzzy Name Matching**: Handles variations in naming (e.g., "NCR" vs "National Capital Region")
- **Administrative Term Normalization**: Removes common prefixes/suffixes like "REGION", "ADMINISTRATIVE"
- **Hierarchical Context**: Matches municipalities within their correct regions and provinces
- **Special Character Handling**: Handles accented characters and encoding variations

### Error Handling
- **Data Validation**: Checks for negative values and invalid formats
- **Graceful Failures**: Continues processing even if individual records fail
- **Detailed Logging**: Comprehensive logs for debugging and monitoring
- **Progress Reporting**: Shows progress every 50 municipalities for large datasets

### Comprehensive Statistics
After seeding, you'll see detailed statistics like:
```
=== Population Data Seeding Results ===
Regions - Matched: 17, Not Found: 0, Errors: 0
Provinces - Matched: 81, Not Found: 2, Region Not Found: 0, Errors: 0  
Municipalities - Matched: 1634, Not Found: 45, Region Not Found: 3, Errors: 2
Summary - Total Matched: 1732, Total Not Found: 50, Total Errors: 2
Success Rate: 97.08%
```

## Data Mapping

The seeder maps your Excel data to these entity properties:

### Region Entity
- `TotalPopulation` ? TotalPopulation (Column B)
- `HouseholdPopulation` ? HouseholdPopulation (Column C)  
- `HouseholdCount` ? NumberOfHouseholds (Column D)

### Province Entity  
- `TotalPopulation` ? TotalPopulation (Column C)
- `HouseholdPopulation` ? HouseholdPopulation (Column D)
- `HouseholdCount` ? NumberOfHouseholds (Column E)

### Municipality Entity
- `TotalPopulation` ? TotalPopulation (Column D)
- `HouseholdPopulation` ? HouseholdPopulation (Column E)
- `HouseholdCount` ? NumberOfHouseholds (Column F)

## Troubleshooting

### Common Issues

1. **"Region not found"**: The region name in your Excel doesn't closely match any existing region
   - Check the region names in your database vs Excel
   - The fuzzy matching requires at least 70% similarity

2. **"Municipality not found in region"**: The municipality exists but not in the expected region
   - Verify the hierarchical relationship (Municipality ? Province ? Region)
   - Check that the region name matches exactly

3. **"Province not found in region"**: The province exists but not in the expected region
   - Verify the province belongs to the specified region
   - Check for spelling variations or administrative changes

4. **Excel file issues**: Problems reading the Excel file
   - Ensure the file is not open in Excel when the application starts
   - Check that sheet names are exactly "Regions", "Provinces", and "Municipalities"
   - Verify the file is named `census_population_data.xlsx` and in the root directory

### Name Matching Issues

The seeder handles common name variations:

```
Excel: "Manila"           Database: "City of Manila"          ? Matches
Excel: "Las Piñas"        Database: "Las Pinas"               ? Matches  
Excel: "NCR"              Database: "National Capital Region" ? Matches
Excel: "ARMM"             Database: "BARMM"                   ?? May not match
```

### Debugging Failed Matches

When municipalities don't match, the system provides detailed logging:
```
Municipality 'Sulu' not found in region 'BARMM'. 
Available municipalities in this region (showing first 10): 
[Bongao, Jolo, Parang, Siasi, Sitangkai, Tandubas, Turtle Islands, ...]
```

### Best Practices

1. **File Placement**: Always place the file as `census_population_data.xlsx` in the root directory
2. **Backup First**: Always backup your database before running with new census data
3. **Check Logs**: Review the detailed logs to understand matching results
4. **Data Validation**: Ensure your Excel data doesn't contain summary rows or province names in municipality columns
5. **Run Once**: Population data rarely changes - the seeder runs automatically during startup

## Example Success Scenarios

### Typical Success Rates
- **Regions**: 95-100% (usually perfect matches)
- **Provinces**: 90-98% (occasional administrative boundary changes)
- **Municipalities**: 85-95% (varies by data source and naming conventions)

### What Gets Logged
```
[Info] Starting population data seeding from Excel file: C:\path\census_population_data.xlsx
[Info] Available sheets: Regions, Provinces, Municipalities
[Info] Processing 17 regions from Excel sheet
[Debug] Region matched: 'National Capital Region (NCR)' ? 'NCR' (Pop: 13,484,462, HH: 2,849,469)
[Info] Municipalities progress: 50/1649 (48 matched, 2 not found, 0 region not found, 0 errors)
[Info] Population data seeding completed successfully!
```

## Dependencies

This seeder uses:
- **ClosedXML.Excel**: For Excel file processing (already in your project)
- **Entity Framework Core**: For database operations  
- **Microsoft.Extensions.Logging**: For comprehensive logging
- **NameMatcher Helper**: For intelligent fuzzy name matching

## Population-Aware Analytics

Once your population data is seeded, the analytics system automatically uses this data for:

- **Population-based CMTS scoring**: Coverage calculations per capita
- **Urban vs Rural classification**: Different infrastructure requirements based on population density  
- **Household coverage estimates**: Actual percentage of households with broadband access
- **Government-grade reporting**: CMTS-compliant coverage statistics
- **Service gap analysis**: Identifying underserved areas based on population metrics

The seeder provides a robust way to import your census data directly from Excel files while maintaining the same simple, clean pattern as your existing seeders and integrating seamlessly with your analytics capabilities.