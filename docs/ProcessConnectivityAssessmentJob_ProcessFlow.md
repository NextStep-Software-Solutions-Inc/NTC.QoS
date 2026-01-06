# Process Connectivity Assessment Job - Detailed Process Flow

## Overview
The ProcessConnectivityAssessmentJob is a Hangfire background job that generates connectivity assessments for geographic locations (Region, Province, Municipality, Barangay) by calculating infrastructure statistics, speed test metrics, and CMTS scores.

---

## 1. Main Job Execution Flow

### Purpose
Orchestrates the entire connectivity assessment process from location discovery to database persistence.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: ExecuteAsync] --> B[Initialize Job Context]
    B --> C[Extract Location IDs from Request]
    C --> D{Hierarchical Processing Enabled?}
    D -->|Yes| E[Discover Child Locations Recursively]
    D -->|No| F[Use Primary Locations Only]
    E --> G[Combine Primary & Hierarchical Locations]
    F --> G
    G --> H{Has Barangay Locations?}
    H -->|Yes| I{Batch Wireless Coverage Computation}
    I --> J[Pre-compute Wireless Data for All Barangays]
    H -->|No| K[Skip Wireless Pre-computation]
    J --> L[Process Hierarchical Locations First]
    K --> L
    L --> M[Process Primary Locations]
    M --> N[Verify Database Persistence]
    N --> O[Calculate Success Rate]
    O --> P{All Locations Saved?}
    P -->|Yes| Q[Update Job Status: Completed]
    P -->|No| R[Log Missing Assessments Warning]
    R --> Q
    Q --> S[End]
    
    ERROR[Exception Caught] --> T[Log Error Details]
    T --> U[Update Job Status: Failed]
    U --> V[Rethrow Exception]
    V --> S
```

### Detailed Steps

#### Phase 1: Initialization
1. Extract job ID, user ID, and start time
2. Parse location IDs and types from request
3. Initialize progress tracking variables

#### Phase 2: Location Discovery
- If hierarchical processing enabled:
  - Discover all child locations up to specified depth
  - Build complete location hierarchy (Region → Province → Municipality → Barangay)
- Group locations by type

#### Phase 3: Wireless Coverage Pre-computation
- If barangays are present:
  - Load all barangay boundaries and metadata
  - Call `WirelessCoverageComputationService.ComputeBatchWirelessCoverageAsync()`
  - Store results in dictionary for fast lookup during processing
- Why? Prevents repeated wireless queries for each barangay

#### Phase 4: Batch Processing
- Process hierarchical locations first (if any)
- Process primary locations second
- Each processing call handles its own batching and persistence

#### Phase 5: Verification & Completion
- Verify all expected assessments exist in database
- Calculate success rate and log discrepancies
- Update job status and duration

---

## 2. Location Processing (ProcessLocationsSequentially)

### Purpose
Processes locations in batches with optimized persistence and progress tracking.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: ProcessLocationsSequentially] --> B[Determine Batch Size by Location Type]
    B --> C[Pre-load Existing Assessments]
    C --> D[Initialize Tracking Variables]
    D --> E{For Each Batch}
    E --> F{For Each Location in Batch}
    F --> G[Process Single Location]
    G --> H{Assessment Generated?}
    H -->|Yes| I[Add to Batch Collection]
    H -->|No| J[Track as Skipped/Failed]
    I --> K{More Locations in Batch?}
    J --> K
    K -->|Yes| F
    K -->|No| L{Batch Has Assessments?}
    L -->|Yes| M[Save Batch to Database]
    L -->|No| N[Skip Save]
    M --> O{Save Successful?}
    O -->|Yes| P[Verify Persistence]
    O -->|No| Q[Log Timeout/Error]
    P --> R[Update Progress]
    Q --> R
    N --> R
    R --> S{Memory Check Needed?}
    S -->|Yes| T[Perform GC & Log Memory]
    S -->|No| U{More Batches?}
    T --> U
    U -->|Yes| E
    U -->|No| V[Return Results & Skipped Count]
    V --> W[End]
    
    CANCEL[Cancellation Requested] --> X[Log Cancellation]
    X --> Y[Return Partial Results]
    Y --> W
```

### Batch Size Configuration

| Location Type | Batch Size | Rationale |
|--------------|-----------|-----------|
| Regional | 1 | Very large spatial queries, 1K-4K children |
| Provincial | 1 | Reduced for timeout prevention |
| Municipality | 1 | Faster commits, timeout prevention |
| Barangay | 3 | Leaf level, most numerous |

### Key Features

1. **Optimistic vs Pessimistic Counting**
   - Processing count: Items processed (optimistic)
   - Saved count: Items persisted (pessimistic, verified)

2. **Batch Persistence Strategy**
   - Accumulate assessments in memory
   - Save entire batch in single transaction
   - Verify database write success
   - Track failures for reporting

3. **Progress Reporting**
   - Report every 5 items for smooth UI updates
   - Calculate processing rate (items/sec)
   - Estimate time remaining

4. **Memory Management**
   - Check memory every 5 batches
   - Force garbage collection when needed
   - Log memory usage for monitoring

---

## 3. Single Location Processing (ProcessSingleLocationForBatch)

### Purpose
Generates or updates assessment for a single location with deferred persistence.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: ProcessSingleLocationForBatch] --> B{Existing Assessment Found?}
    B -->|Yes| C{Force Recalculate?}
    B -->|No| D[Generate New Assessment]
    C -->|Yes| D
    C -->|No| E{Recent Assessment?}
    E -->|Yes| F[Skip - Return null]
    E -->|No| G[Update Existing Assessment]
    D --> H[Create Assessment Entity]
    G --> I[Update Assessment Fields]
    H --> J[Build Provider Entities]
    I --> J
    J --> K[Return Assessment Tuple]
    K --> L[End]
    F --> L
```

### Assessment Generation Steps

1. Check for existing assessment in pre-loaded cache
2. Determine if recalculation needed
3. If new assessment:
   - Call `GenerateNewAssessment()`
   - Create providers list
4. If update:
   - Call `UpdateExistingAssessment()`
   - Rebuild providers list
5. Return (Assessment, Providers, LocationName) tuple

---

## 4. Assessment Generation (GenerateNewAssessment)

### Purpose
Computes complete connectivity assessment with all CMTS components.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: GenerateNewAssessment] --> B[Fetch Location Details]
    B --> C[Calculate Infrastructure Stats]
    C --> D[Calculate Speed Test Stats]
    D --> E[Analyze Technology Breakdown]
    E --> F[Analyze Band Frequency]
    F --> G[Analyze Technology-Band Breakdown]
    G --> H[Calculate CMTS Scores]
    H --> I[Calculate Population-Aware Coverage]
    I --> J[Determine Connectivity Status]
    J --> K[Determine Reliable Connectivity]
    K --> L{Has Wireless Coverage Data?}
    L -->|Yes| M[Store Wireless Coverage JSON]
    L -->|No| N[Leave Wireless Field Null]
    M --> O[Build Complete Assessment Entity]
    N --> O
    O --> P[Set Timestamps & User]
    P --> Q[Return Assessment]
    Q --> R[End]
```

### Data Collection Sequence

```mermaid
sequenceDiagram
    participant Job as Assessment Job
    participant Location as Location Repository
    participant Infra as Infrastructure Service
    participant Speed as Speed Test Service
    participant Tech as Technology Service
    participant CMTS as CMTS Calculator
    
    Job->>Location: GetLocationDetails(id, type)
    Location-->>Job: (name, boundary, center, area, pop)
    
    Job->>Infra: CalculateInfrastructureStats(boundary)
    Note over Infra: Checks cache first
    Infra->>Infra: Query cells, NAPs, WiFi
    Infra-->>Job: InfrastructureStatsDto
    
    Job->>Speed: CalculateSpeedTestStats(boundary)
    Note over Speed: Checks cache first
    Speed->>Speed: Query speedtests, aggregate
    Speed-->>Job: SpeedStatsDto
    
    Job->>Tech: AnalyzeTechnologyGenerations(boundary)
    Tech->>Tech: Query cellulars by generation
    Tech-->>Job: TechnologyBreakdownDto
    
    Job->>CMTS: CalculateCMTSScores(infra, speed, area, pop)
    CMTS->>CMTS: Score 5 components (0-3 each)
    CMTS-->>Job: CMTSScoresDto (total 0-15)
    
    Job->>Job: Build ConnectivityAssessment
```

---

## 5. Infrastructure Statistics Calculation

### Purpose
Calculates accurate infrastructure asset counts using optimized spatial queries.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: CalculateInfrastructureStatsAsync] --> B{Force Recalculate?}
    B -->|No| C{Cache Hit?}
    C -->|Yes| D[Return Cached Result]
    B -->|Yes| E[Clear Cache]
    C -->|No| E
    E --> F[Call CalculateInfrastructureWithBoundaryOptimization]
    F --> G{Location Type?}
    G -->|Regional/Provincial| H[Use Subquery Filtering]
    G -->|Municipal/Barangay| I[Use Envelope Filtering]
    H --> J[Query Cell COUNT]
    I --> J
    J --> K[Query NAP COUNT]
    K --> L[Query WiFi COUNT]
    L --> M[Query Wireless Validations]
    M --> N[Query Distinct Provider Names]
    N --> O[Query Technology Breakdown]
    O --> P[Calculate Provider Breakdown]
    P --> Q[Build InfrastructureStatsDto]
    Q --> R[Cache Result 24 hours]
    R --> S[Return Result]
    S --> T[End]
    D --> T
```

### Query Optimization Strategy

**Regional/Provincial (Large Areas)**
- Uses: Subquery-based boundary filtering
- No geometry materialization
- Prevents timeout on complex boundaries
- Example:
  ```sql
  WHERE location.Within(
    (SELECT boundary FROM regions WHERE id = @regionId)
  )
  ```

**Municipal/Barangay (Small Areas)**
- Uses: Envelope filtering
- Single boundary materialization
- Faster for simple geometries
- Example:
  ```sql
  WHERE location.Within(@boundary)
  AND location.Intersects(@envelope)
  ```

---

## 6. Speed Test Statistics Calculation

### Purpose
Aggregates speed test data with database-level calculations to avoid memory exhaustion.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: CalculateSpeedTestStatsAsync] --> B{Force Recalculate?}
    B -->|No| C{Cache Hit?}
    C -->|Yes| D[Return Cached Result]
    B -->|Yes| E[Clear Cache]
    C -->|No| E
    E --> F{Boundary Exists?}
    F -->|Yes| G{Location Type?}
    F -->|No| H[Use Center Point Fallback]
    G -->|Regional/Provincial| I[Apply Subquery Filter]
    G -->|Municipal/Barangay| J[Apply Envelope Filter]
    I --> K[Add Date Range Filters]
    J --> K
    H --> K
    K --> L[Count Total Records]
    L --> M{Count > 10000?}
    M -->|Yes| N[Use Database Aggregation]
    M -->|No| O[Load to Memory & Calculate]
    N --> P[Database AVG, PERCENTILE_CONT]
    O --> Q[In-Memory Median Calculation]
    P --> R[Build SpeedStatsDto]
    Q --> R
    R --> S[Cache Result 12 hours]
    S --> T[Return Result]
    T --> U[End]
    D --> U
```

### Database Aggregation (Large Datasets)

For regions with millions of speedtest records:
- Uses `AVG()` for averages
- Uses `PERCENTILE_CONT(0.5)` for median approximation
- Prevents PostgreSQL shared memory exhaustion (Error 53100)
- All calculations stay in database

---

## 7. CMTS Scoring System

### Purpose
Calculates 5-component Connectivity Maturity and Technology Score (0-15 total points).

### CMTS Components

```mermaid
flowchart TD
    A[Calculate CMTS Scores] --> B[1. Fiber Coverage 0-3]
    A --> C[2. Internet Access 0-3]
    A --> D[3. Technology Type 0-3]
    A --> E[4. Quality of Service 0-3]
    A --> F[5. Coverage Availability 0-3]
    
    B --> G[Check NAP Count & Providers]
    C --> H[Check Speed Tests & Median Speed]
    D --> I[Check 5G/4G/3G/2G Mix]
    E --> J[Check Latency & Signal Strength]
    F --> K[Check Infrastructure Density vs Population]
    
    G --> L[Sum All Scores]
    H --> L
    I --> L
    J --> L
    K --> L
    
    L --> M{Total Score?}
    M -->|11-15| N[Status: SERVED]
    M -->|6-10| O[Status: UNDERSERVED]
    M -->|0-5| P[Status: UNSERVED]
    
    N --> Q[Return Scores & Status]
    O --> Q
    P --> Q
```

### Scoring Logic

#### 1. Fiber Coverage (0-3 points)
- 3 points: Multiple major telcos (Globe, Smart, DITO)
- 2 points: 1 major telco present
- 1 point: Local/VAS providers only
- 0 points: No fiber infrastructure

#### 2. Internet Access (0-3 points)
- 3 points: Median ≥25 Mbps, reliable
- 2 points: Median ≥25 Mbps, unreliable OR ≥10 Mbps reliable
- 1 point: Median ≥10 Mbps, unreliable
- 0 points: <10 Mbps or no data

#### 3. Technology Type (0-3 points)
- 3 points: Fiber OR 5G present
- 2 points: 4G present, not legacy-dominated
- 1 point: Modern tech present but legacy-dominated
- 0 points: Only 2G/3G

#### 4. Quality of Service (0-3 points)
- 3 points: 5G + low latency (≤20ms) + high speed (≥100 Mbps)
- 2 points: Low latency + good signal + consistent speeds
- 1 point: Some quality indicators met
- 0 points: High latency (>100ms) or poor signal

#### 5. Coverage Availability (0-3 points)
- 3 points: ≥100% estimated population coverage
- 2 points: 40-99% estimated population coverage
- 1 point: <40% estimated population coverage
- 0 points: No infrastructure

### Coverage Estimation Formula

```
Coverage = (TotalAssets × CoverageRadiusPerAsset) / Population

Where:
- Urban areas: 500 people per asset
- High density: 1000 people per asset
- Rural: 2000 people per asset
```

---

## 8. Batch Save Process

### Purpose
Persists assessments to database with transaction safety and verification.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start Batch Save] --> B[Begin Transaction]
    B --> C{For Each Assessment in Batch}
    C --> D{Existing in DB?}
    D -->|Yes| E[Update Entity]
    D -->|No| F[Add New Entity]
    E --> G[Update Providers]
    F --> H[Add Providers]
    G --> I{More Assessments?}
    H --> I
    I -->|Yes| C
    I -->|No| J[Set Timeout 5 minutes]
    J --> K[Call SaveChangesAsync]
    K --> L{Save Successful?}
    L -->|Yes| M[Verify Persistence]
    L -->|Timeout| N[Log Timeout Error]
    L -->|Exception| O[Log Exception]
    M --> P{Verification Match?}
    P -->|Yes| Q[Return Success]
    P -->|No| R[Log Discrepancy]
    N --> S[Rollback Transaction]
    O --> S
    R --> Q
    S --> T[Return Failure]
    Q --> U[End]
    T --> U
```

### Timeout Handling

- **Batch Save Timeout**: 300 seconds (5 minutes)
- **Verification Timeout**: 60 minutes (10 minutes)
- **On Timeout**: Log error, track failed batch, continue with next batch
- **No Retry**: Job configuration has 0 retry attempts

---

## 9. Database Verification

### Purpose
Confirms that assessments were actually persisted after batch save.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start Verification] --> B[Split Location IDs into Verification Batches]
    B --> C{For Each Verification Batch}
    C --> D[Query Database for Assessment Count]
    D --> E[Add to Running Total]
    E --> F{More Batches?}
    F -->|Yes| C
    F -->|No| G{Total Matches Expected?}
    G -->|Yes| H[Log Success]
    G -->|No| I[Log Discrepancy Warning]
    I --> J[Calculate Missing Count]
    J --> K[Log DATABASE DISCREPANCY]
    H --> L[Return Verified Count]
    K --> L
    L --> M[End]
```

### Discrepancy Detection

- Compares processed count vs database count
- Logs `DATABASE DISCREPANCY DETECTED` if mismatch
- Indicates database write failures
- Critical for identifying timeout/transaction issues

---

## 10. Error Handling & Recovery

### Error Handling Strategy

```mermaid
flowchart TD
    A[Operation Executes] --> B{Success?}
    B -->|Yes| C[Continue Processing]
    B -->|No| D{Error Type?}
    D -->|Cancellation| E[Log Cancellation]
    D -->|Timeout| F[Log Timeout Details]
    D -->|Database Error| G[Log DB Error]
    D -->|Other Exception| H[Log Exception]
    E --> I{Current Level?}
    F --> I
    G --> I
    H --> I
    I -->|Single Location| J[Track as Failed]
    I -->|Batch| K[Log Batch Failure]
    I -->|Job| L[Update Job Status Failed]
    J --> M[Continue Next Location]
    K --> N[Continue Next Batch]
    L --> O[Rethrow for Hangfire]
    M --> C
    N --> C
```

### Recovery Mechanisms

1. **Location-Level Failures**
   - Individual location errors don't stop batch
   - Failed locations tracked and reported
   - Processing continues with next location

2. **Batch-Level Failures**
   - Batch save failures logged with full context
   - Batch marked as failed
   - Processing continues with next batch

3. **Job-Level Failures**
   - Uncaught exceptions update job status to Failed
   - Full error details logged
   - No automatic retry (Attempts = 0)
   - Manual intervention required

---

## 11. Performance Optimizations

### Optimization Summary

| Optimization | Impact | Implementation |
|-------------|--------|----------------|
| Multi-level caching | 50-90% faster | 24h infra, 12h speed cache |
| Batch processing | 10-100x faster | Dynamic batch sizes |
| Subquery filtering | No timeout | Regional/Provincial |
| Envelope filtering | 2-5x faster | Municipal/Barangay |
| Wireless pre-computation | 1000x faster | R-Tree batch mode |
| Database aggregation | Prevents OOM | Large speedtest datasets |
| Memory management | Stable long jobs | GC every 5 batches |

### Caching Strategy

```mermaid
graph TD
    A[Request Data] --> B{Cache Key Exists?}
    B -->|Yes| C{Force Recalculate?}
    C -->|No| D[Return Cached Data]
    C -->|Yes| E[Clear Cache]
    B -->|No| E
    E --> F[Execute Query]
    F --> G[Store in Cache]
    G --> H[Return Fresh Data]
    
    D --> I[Log Cache Hit]
    H --> J[Log Cache Miss]
```

**Cache Keys:**
- Infrastructure: `infra:{locationType}:{locationId}` (24h)
- Speed: `speed:{locationType}:{locationId}` (12h)
- Technology: `tech:{locationType}:{locationId}` (24h)

---

## 12. Job Configuration

### Hangfire Attributes

```csharp
[Queue("connectivity-assessment")]           // Dedicated queue
[AutomaticRetry(Attempts = 0)]              // No retry, fail immediately
[DisableConcurrentExecution(timeout: 14400)] // 4 hours, no parallel runs
```

### Rationale

- **No Retry**: Prevents duplicate assessments, requires manual intervention
- **No Concurrency**: Prevents database exhaustion, ensures serialized execution
- **4 Hour Timeout**: Handles large hierarchical jobs (1000+ locations)

---

## 13. Progress Tracking

### Job Status Updates

```mermaid
stateDiagram-v2
    [*] --> Queued: Job Created
    Queued --> Processing: Job Started
    Processing --> Processing: Progress Updates
    Processing --> Completed: Success
    Processing --> Failed: Exception
    Completed --> [*]
    Failed --> [*]
```

### Tracked Metrics

- **StartTime**: Job execution start
- **EndTime**: Job completion time
- **ProcessingCount**: Currently processing (optimistic)
- **ProcessedCount**: Successfully saved (pessimistic)
- **TotalCount**: Expected total locations
- **ProgressPercentage**: (ProcessedCount / TotalCount) × 100
- **Message**: Human-readable status
- **CurrentStep**: Processing phase description

---

## 14. Integration Points

### Input
- `ConnectivityAssessmentJobRequestDto`
  - Location IDs and types
  - Hierarchical processing settings
  - Date range filters
  - Force recalculate flag
  - User ID

### Output
- Updated `ConnectivityAssessment` records in database
- Updated `ConnectivityAssessmentProvider` records
- Job status updates in Hangfire

### Dependencies
- Location repositories (Region, Province, Municipality, Barangay)
- Infrastructure repositories (Cell, NAP, WiFi, Cellular, WirelessValidation)
- Speedtest repository
- WirelessCoverageComputationService
- Memory cache
- Entity Framework DbContext

---

## 15. Monitoring & Logging

### Key Log Messages

| Level | Message Pattern | Purpose |
|-------|----------------|---------|
| Info | Job started | Job initiation |
| Info | Processing {N} locations | Batch start |
| Info | Batch {X}/{Y} saved {N} | Batch completion |
| Warn | Cancellation requested | Job interruption |
| Error | Batch save TIMEOUT | Database timeout |
| Error | DATABASE DISCREPANCY | Persistence failure |
| Info | Job completed | Job success |

### Performance Metrics Logged

- Processing rate (items/second)
- Batch save duration
- Memory usage (every 5 batches)
- Cache hit/miss ratios
- Total job duration
- Success rate percentage

---

## 16. Common Issues & Solutions

| Issue | Symptom | Solution |
|-------|---------|----------|
| Batch save timeout | "Batch save TIMEOUT after 300s" | Reduce batch size |
| Missing assessments | "DATABASE DISCREPANCY DETECTED" | Check database locks/performance |
| Job cancellation | "Cancellation requested" | Check job timeout settings |
| Memory exhaustion | Slow processing, high memory | Reduce batch size, check GC |
| Slow queries | Long processing times | Check spatial indexes, optimize queries |

---

## 17. Testing Considerations

### Test Scenarios

1. **Single Location**: Process 1 barangay
2. **Small Batch**: Process 10 barangays
3. **Large Batch**: Process 100+ barangays
4. **Hierarchical**: Process region with 1000+ children
5. **Force Recalculate**: Override recent assessments
6. **Empty Data**: Locations with no infrastructure
7. **Timeout Simulation**: Very large regions
8. **Cancellation**: Mid-job termination

### Validation Checks

- All expected assessments created
- CMTS scores within 0-15 range
- Provider counts match infrastructure
- Wireless coverage JSON valid
- No duplicate assessments
- Timestamps accurate
- User context preserved
