# Location and Population Seeding Architecture

## Overview

Population data (Total Population, Household Population, and Household Count) is **directly integrated into the geospatial boundaries seeding pipeline** via `LocationSeederV2` and `StreamingGeoJsonParser`. 

The legacy Excel-based seeder (`census_population_data.xlsx`) and local root directory GeoJSON files (`admin1.geojson` - `admin4.geojson`) have been consolidated. The **authoritative single source of truth** is now **Cloud Blob Storage** (`IBlobStorageService`), which streams boundary and census metrics directly into database entities during initialization or on-demand background jobs.

---

## 1. Cloud Storage Data Source

Seeding data is hosted in the configured Blob Storage provider (Azure Blob Storage / MinIO / Azurite) under the `seeds` container.

### Storage Hierarchy (`seeds` container)

```
seeds/
└── boundaries/
    ├── Regionbdry.geojson         # Admin Level 1 (Regions + Regional Census Metrics)
    ├── Provincebdry.geojson       # Admin Level 2 (Provinces + Provincial Census Metrics)
    ├── Municipalitybdry.geojson   # Admin Level 3 (Municipalities/Cities + Census Metrics)
    └── Barangaybdry.geojson       # Admin Level 4 (Barangays boundaries)
```

No local files in the application root directory or deployment containers are required.

---

## 2. Integrated Population Extraction

GeoJSON feature collections in cloud storage carry administrative attributes alongside polygon geometry coordinates. The `StreamingGeoJsonParser` extracts both spatial boundaries and population metrics in a single pass.

### Supported Feature Attributes

| GeoJSON Property Key | Target Entity Property | Description |
| :--- | :--- | :--- |
| `TOTAL_POPN` or `TOTAL_POP` | `TotalPopulation` (`int?`) | Total resident population |
| `HHOLD_POPN` | `HouseholdPopulation` (`int?`) | Household population |
| `HHOLD_CNT` | `HouseholdCount` (`int?`) | Total number of households |
| `AREA_SQKM` | `Area` (`float`) | Land area in square kilometers |
| `ADM1_PCODE` / `ADM2_PCODE` / etc. | `Code` (`string`) | Standard geographic code |
| `ADM1_EN` / `ADM2_EN` / etc. | `Name` (`string`) | Official geographic name |

### Calculated Entity Metrics

Entities automatically compute derived population metrics based on the imported values:

* **Population Density** ($\text{persons} / \text{km}^2$):
  $$\text{PopulationDensity} = \frac{\text{TotalPopulation}}{\text{Area}}$$
* **Average Household Size**:
  $$\text{AverageHouseholdSize} = \frac{\text{HouseholdPopulation}}{\text{HouseholdCount}}$$

---

## 3. Streaming Seeder Pipeline (`LocationSeederV2`)

To handle large boundary datasets without high memory allocation (allocating less than 50 MB RAM for 2.5 GB of GeoJSON data), the architecture utilizes a streaming strategy:

```
[Cloud Blob Storage (seeds/boundaries/*)]
              │ (Stream)
              ▼
   [StreamingGeoJsonParser]
    - Streams features via Newtonsoft.Json JsonTextReader
    - Parses boundary polygons (NetTopologySuite)
    - Extracts TOTAL_POPN, HHOLD_POPN, HHOLD_CNT
              │
              ▼
    [LocationTransformers]
    - RegionTransformer / ProvinceTransformer / MunicipalityTransformer
    - Maps properties to Domain Entities
              │
              ▼
   [BatchedLocationWriter]
    - Bulk inserts / updates in configurable batches
    - Preserves PostGIS spatial topology
```

### Seeding Hierarchy Order

1. **Regions (`Regionbdry.geojson`)**: Seeded first (independent roots).
2. **Provinces (`Provincebdry.geojson`)**: Mapped to parent `RegionId` via `ADM1_PCODE`.
3. **Municipalities (`Municipalitybdry.geojson`)**: Mapped to parent `ProvinceId` via `ADM2_PCODE`.
4. **Barangays (`Barangaybdry.geojson`)**: Mapped to parent `MunicipalityId` via `ADM3_PCODE` (population fields null at barangay level).

---

## 4. Execution & Triggering

### Automatic Startup Seeding
Managed via dependency injection in `NTC.QoS.Infrastructure`:
* Controlled by configuration: `DatabaseOptions.ForceReseed`.
* If complete data is already present in PostgreSQL, the seeder skips automatically to avoid duplicate processing.

### On-Demand Reseeding (Super Admin API)
Reseeding can be triggered via `SeedController` which enqueues a Hangfire background job (`ProcessLocationSeedJob`):

```http
POST /seed
Authorization: Bearer <SUPER_ADMIN_TOKEN>
Content-Type: application/json

{
  "regionEntity": true,
  "provinceEntity": true,
  "municipalityEntity": true,
  "barangayEntity": false,
  "forceReseed": false
}
```

---

## 5. Republic Act No. 12000 (NIR) Compliance

With the establishment of the **Negros Island Region (NIR)** under RA 12000:
1. **Cloud Boundaries Update**: Update `Regionbdry.geojson` to include `PH18` (NIR) and recalculated `PH07` (Region VII) and `PH06` (Region VI).
2. **Province & Municipality Attribution**: Update `Provincebdry.geojson` and `Municipalitybdry.geojson` in storage so that Negros Occidental, Negros Oriental, and Siquijor features point to `ADM1_PCODE: "PH18"`.
3. **Automatic Population Roll-Up**: Because population data is embedded directly in the storage GeoJSON files, running the seeder or database patch immediately assigns the correct population to NIR, Region VII, and Region VI.
