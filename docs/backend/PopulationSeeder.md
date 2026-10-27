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

## 5. Architectural Decision: GeoJSON vs. Excel Population Sources

Historically, population figures were imported via a secondary startup pass using an Excel spreadsheet (`census_population_data.xlsx`) and `ClosedXML`. This design was deprecated and removed (commit `ba0b7f8b`) in favor of unified GeoJSON attributes.

### Why Seeding Directly from Excel is High Risk
1. **The Split-Brain Hazard**: Decoupling demographic attributes from spatial boundary polygons leaves the database prone to partial syncs, missing geometries, or orphaned census records.
2. **Fuzzy String Matching Failures**: Excel spreadsheets identify administrative units by English strings (*"City of Manila"*, *"Las Piñas"*), requiring fuzzy string matching with a 70% threshold. In practice, this yielded an 85%–95% match rate, resulting in 5%–15% of LGUs having missing or incorrect demographic metrics due to spelling variations, diacritics, or naming conventions.
3. **PCODE Determinism**: GeoJSON boundary features are indexed on official government standard PCODEs (`ADM1_PCODE`, `ADM2_PCODE`, `ADM3_PCODE`), guaranteeing 100% deterministic spatial and foreign key joins.
4. **Stale Hierarchies**: Pre-existing Excel census files do not reflect newly enacted administrative divisions (such as RA 12000 NIR), risking accidental rollback of regional totals to legacy configurations.

### Standard Operating Procedure (SOP) for New Census Releases
When official census updates (e.g., from Philippine Statistics Authority / PSA) become available in tabular Excel/CSV format:
1. **Pre-Processing (Offline)**: Do not parse raw Excel files inside the live .NET API. Instead, run an offline Python/GIS script to join the PSA census table to GeoJSON features using standard PCODEs.
2. **Validation**: Verify that all administrative codes match 1:1 and that calculated regional/provincial summaries equal official national totals.
3. **Storage Upload**: Upload the enriched GeoJSON files to Cloud Blob Storage (`seeds/boundaries/`).
4. **Seeding / Reseeding**: Trigger `LocationSeederV2` (or the Hangfire seed endpoint) to stream updated attributes into PostgreSQL in a single deterministic pass.

---

## 6. Republic Act No. 12000 (NIR) Compliance & Migration Runbook

Republic Act No. 12000 officially established the **Negros Island Region (NIR / Region XVIII, code `PH18`)**, transferring:
* **Negros Occidental (`PH06045`)** from Region VI to NIR.
* **Negros Oriental (`PH07046`)** and **Siquijor (`PH07061`)** from Region VII to NIR.

### Resulting Administrative Counts
* **NIR (`PH18`)**: 3 provinces, 63 municipalities/cities, 1,353 barangays.
* **Region VII (`PH07`)**: 2 provinces (Cebu, Bohol).
* **Region VI (`PH06`)**: 5 provinces (Aklan, Antique, Capiz, Guimaras, Iloilo).

### Production Migration Strategy: DB In-Place Script vs. GeoJSON Reseed

For production environments, **the in-place transactional SQL script is the required, low-risk approach**:

| Factor | Option A: Full GeoJSON Reseed | Option B: In-Place SQL (`migrate_db_ra12000_nir.sql`) |
| :--- | :--- | :--- |
| **Execution Time** | 10 to 30+ minutes | **< 2 seconds** |
| **Blast Radius** | 42,000+ barangay, municipality, province rows | **3 province rows, 3 region rows** |
| **Foreign Key Safety** | High risk of violating `UserRegion`, `Role`, `File` FKs | **Zero risk** (preserves all existing UUIDs) |
| **Downtime** | Requires maintenance window | **Zero downtime** (atomic transaction) |

Because the QoS backend schema is relational (`Barangays` $\to$ `Municipalities` $\to$ `Provinces` $\to$ `Regions`), pointing `Provinces."RegionId"` to NIR on the 3 transferred provinces automatically cascades all 63 municipalities and 1,353 barangays into NIR without modifying a single municipality or barangay row.

### Pipeline Tooling in Repository

* **Database Migration**: [`scripts/migrate_db_ra12000_nir.sql`](file:///c:/Users/almau/NTC.QoS-Backend/scripts/migrate_db_ra12000_nir.sql)
  * Idempotent SQL script that creates NIR (`PH18`), updates the 3 province foreign keys, and uses PostGIS `ST_Union` to recalculate Region VI, VII, and NIR boundaries and demographic metrics atomically.
* **GeoJSON Transformation Pipeline**: [`scripts/transform_geojson_ra12000.py`](file:///c:/Users/almau/NTC.QoS-Backend/scripts/transform_geojson_ra12000.py)
  * Generates updated GeoJSON boundary files (`admin1` to `admin4`) with Shapely geometry unions and PCODE reassignments for seeder synchronization.
* **Staged GeoJSON Applicator**: [`scripts/apply_staged_geojson.py`](file:///c:/Users/almau/NTC.QoS-Backend/scripts/apply_staged_geojson.py)
  * Stages transformed files with pre-migration backups (`.bak_pre_ra12000`) and dry-run simulation support.

### Verification Queries
Post-migration verification can be executed via:

```sql
-- 1. Province counts per region (Expected: PH06 = 5, PH07 = 2, PH18 = 3)
SELECT r."Code", r."Name", COUNT(p."Id") AS "ProvinceCount"
FROM "Regions" r
LEFT JOIN "Provinces" p ON p."RegionId" = r."Id" AND NOT p."IsDeleted"
WHERE r."Code" IN ('PH06', 'PH07', 'PH18')
GROUP BY r."Code", r."Name"
ORDER BY r."Code";

-- 2. Verify PostGIS geometry validity
SELECT "Code", "Name", ST_IsValid("Boundaries") AS "GeometryIsValid", ST_GeometryType("Boundaries") AS "GeomType"
FROM "Regions"
WHERE "Code" IN ('PH06', 'PH07', 'PH18')
ORDER BY "Code";
```
