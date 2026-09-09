# Wireless Coverage Computation Service - Detailed Process Flow

## Overview
The WirelessCoverageComputationService computes wireless coverage data for barangays by aggregating data from multiple sources (cellulars, wireless validations, speedtests) and organizing it by provider and technology generation.

---

## 1. Single Barangay Coverage Computation

### Purpose
Computes wireless coverage for a single barangay and returns a JSON summary.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: ComputeBarangayWirelessCoverageAsync] --> B{Boundary Exists?}
    B -->|No| C[Log Warning]
    C --> D[Return null]
    B -->|Yes| E[Query Cellulars Within Boundary]
    E --> F[Query Wireless Validations for Barangay]
    F --> G[Query Speedtests Within Boundary]
    G --> H[Extract Unique Provider IDs]
    H --> I{For Each Provider}
    I --> J[Filter Cellulars by Provider]
    J --> K[Filter Validations by Provider]
    K --> L[Filter Speedtests by Provider]
    L --> M[Build Unique Technologies Set]
    M --> N[Count Technologies per Generation 2G/3G/4G/5G]
    N --> O{Has Speedtests?}
    O -->|Yes| P[Calculate Avg/Median Speeds]
    O -->|No| Q[Skip Speed Stats]
    P --> R[Build Provider Coverage DTO]
    Q --> R
    R --> S[Store Provider DTO]
    S --> T{More Providers?}
    T -->|Yes| I
    T -->|No| U[Aggregate All Providers into Summary]
    U --> V[Determine Overall Median Technology]
    V --> W[Determine Best Available Technology]
    W --> X[Serialize Summary to JSON]
    X --> Y[Return JSON String]
    Y --> Z[End]
    D --> Z
```

### Detailed Steps

1. **Input Validation**
   - Check if boundary geometry exists
   - If missing, log warning and return null

2. **Data Collection (3 Parallel Queries)**
   - Query all cellulars within barangay boundary with spatial filter
   - Query all wireless validations for the specific barangay ID
   - Query all speedtests within barangay boundary with spatial filter

3. **Provider Grouping**
   - Extract unique provider IDs from all three data sources
   - Create union of providers across cellulars, validations, and speedtests

4. **Per-Provider Processing**
   - Filter each data source by provider ID
   - Build unique technology set (cross-source deduplication)
   - Count technologies per generation (2G, 3G, 4G, 5G)
   - Calculate speedtest statistics (average, median download/upload)
   - Determine median technology from speedtests
   - Build BarangayWirelessCoverageDto

5. **Summary Aggregation**
   - Collect all provider DTOs
   - Calculate totals (providers, cellulars, validations, speedtests)
   - Determine overall median technology across providers
   - Determine best available technology (prioritize 5G > 4G > 3G > 2G)

6. **Serialization**
   - Serialize BarangayWirelessSummaryDto to compact JSON
   - Return JSON string for storage

---

## 2. Batch Barangay Coverage Computation (R-Tree Optimized)

### Purpose
Computes wireless coverage for multiple barangays efficiently using bulk queries and spatial indexing.

### Performance
- **50-100x faster** than nested loops
- Handles **1,000+ barangays** without timeout
- Uses **R-Tree spatial indexing** for O(n log n) complexity

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: ComputeBatchWirelessCoverageAsync] --> B{Region Boundary & Barangays Exist?}
    B -->|No| C[Log Warning & Return Empty]
    B -->|Yes| D[Step 1: Query ALL Cellulars in Region]
    D --> E[Step 2: Query ALL Wireless Validations]
    E --> F[Step 3: Query ALL Speedtests in Region]
    F --> G[Step 4: Build R-Tree Spatial Index]
    G --> H[Index Barangay Boundaries in R-Tree]
    H --> I[Build R-Tree Index]
    I --> J[Step 5: Group Cellulars by Barangay]
    J --> K{For Each Cellular}
    K --> L[Query R-Tree for Candidate Barangays]
    L --> M{Point Within Boundary?}
    M -->|Yes| N[Assign to Barangay]
    M -->|No| O[Skip]
    N --> P{More Cellulars?}
    O --> P
    P -->|Yes| K
    P -->|No| Q[Group Validations by Barangay ID]
    Q --> R[Step 6: Group Speedtests by Barangay]
    R --> S{For Each Speedtest}
    S --> T[Query R-Tree for Candidate Barangays]
    T --> U{Point Within Boundary?}
    U -->|Yes| V[Assign to Barangay]
    U -->|No| W[Skip]
    V --> X{More Speedtests?}
    W --> X
    X -->|Yes| S
    X -->|No| Y[Step 7: Process Each Barangay]
    Y --> Z{For Each Barangay}
    Z --> AA[Call ComputeCoverageForBarangay]
    AA --> AB[Aggregate Provider Data In-Memory]
    AB --> AC[Build Coverage Summary]
    AC --> AD[Serialize to JSON]
    AD --> AE[Store in Result Dictionary]
    AE --> AF{More Barangays?}
    AF -->|Yes| Z
    AF -->|No| AG[Log Performance Metrics]
    AG --> AH[Return Dictionary of Results]
    AH --> AI[End]
    C --> AI
```

### Detailed Steps

#### Step 1: Bulk Data Loading
- **Load ALL cellulars** in region with single query (includes provider, technology, location)
- **Load ALL wireless validations** for all barangays with single query
- **Load ALL speedtests** in region with single query

**Performance Note**: These 3 queries replace thousands of individual queries, reducing database load dramatically.

#### Step 2: Build R-Tree Spatial Index
- Create STRtree (Sort-Tile-Recursive tree) for barangay boundaries
- Insert each barangay boundary envelope into the tree
- Build optimized index structure

**Why R-Tree?**
- Efficiently finds spatial candidates using bounding box queries
- Reduces spatial checks from O(n²) to O(n log n)
- Essential for handling 1,000+ barangays

#### Step 3: Spatial Assignment (R-Tree Optimization)
For cellulars and speedtests:
1. Query R-Tree with point location to get candidate barangays
2. Validate actual containment using precise geometry check
3. Assign to matching barangay

For wireless validations:
- Direct assignment using BarangayId (no spatial check needed)

#### Step 4: In-Memory Aggregation
For each barangay:
1. Retrieve grouped cellulars, validations, speedtests
2. Call `ComputeCoverageForBarangay` with in-memory data
3. Process providers, technologies, and statistics
4. Generate JSON summary

#### Step 5: Result Collection
- Store JSON summaries in dictionary (barangayId → JSON)
- Log performance metrics (processing time, assignment rates)
- Return complete result set

---

## 3. Helper Method: ComputeCoverageForBarangay

### Purpose
Processes pre-grouped in-memory data to generate coverage summary for a single barangay.

### Process Flow Diagram

```mermaid
flowchart TD
    A[Start: ComputeCoverageForBarangay] --> B[Receive Grouped Data]
    B --> C[Extract Unique Provider IDs]
    C --> D{For Each Provider}
    D --> E[Filter Cellulars by Provider]
    E --> F[Filter Validations by Provider]
    F --> G[Filter Speedtests by Provider]
    G --> H[Build Unique Technologies HashSet]
    H --> I[Add Technologies from Cellulars]
    I --> J[Add Technologies from Validations]
    J --> K[Add Technologies from Speedtests]
    K --> L[Normalize & Count by Generation]
    L --> M[Calculate Technology Stats]
    M --> N{Has Speedtests?}
    N -->|Yes| O[Calculate Speed Statistics]
    N -->|No| P[Set Speed Stats to null]
    O --> Q[Build Provider Coverage DTO]
    P --> Q
    Q --> R[Add to Provider List]
    R --> S{More Providers?}
    S -->|Yes| D
    S -->|No| T[Build Summary DTO]
    T --> U[Calculate Aggregated Totals]
    U --> V[Determine Overall Median Tech]
    V --> W[Determine Best Technology]
    W --> X[Serialize to JSON]
    X --> Y[Return JSON String]
    Y --> Z[End]
```

### Key Features

1. **Cross-Source Technology Aggregation**
   - Combines technologies from cellulars, validations, and speedtests
   - Deduplicates using HashSet (LTE + FDD-LTE counted as separate 4G techs)
   - Normalizes generation names to 2G/3G/4G/5G standard format

2. **Generation Counting**
   - Count2G, Count3G, Count4G, Count5G
   - TotalTechnologyCount
   - ModernCoverageRatio = (4G + 5G) / Total
   - IsLegacyDominated = (2G + 3G) > (4G + 5G)

3. **Speedtest Processing**
   - Average download/upload speeds
   - Median download/upload speeds (using sorted lists)
   - Median technology determination

4. **Summary Construction**
   - Total wireless providers
   - Total cellular/validation/speedtest counts
   - Overall median technology (most common across providers)
   - Best technology available (5G > 4G > 3G > 2G)

---

## 4. Data Flow Architecture

```mermaid
graph LR
    A[Database] -->|Cellulars| B[Single Query]
    A -->|Validations| B
    A -->|Speedtests| B
    B --> C[R-Tree Index]
    C --> D[Spatial Assignment]
    D --> E[Grouped by Barangay]
    E --> F[In-Memory Processing]
    F --> G[JSON Summaries]
    G --> H[Result Dictionary]
```

---

## 5. Performance Characteristics

### Single Barangay Mode
- **Database Queries**: 3 (cellulars, validations, speedtests)
- **Time Complexity**: O(C + V + S) where C, V, S are counts per barangay
- **Space Complexity**: O(P) where P is provider count
- **Use Case**: Real-time computation for individual barangay

### Batch Mode (R-Tree)
- **Database Queries**: 3 (entire region, all barangays)
- **Time Complexity**: O((C + S) × log(B) + B × P) where B is barangay count
- **Space Complexity**: O(C + V + S + B)
- **Use Case**: Bulk processing for regional assessments

### Performance Comparison
| Operation | Single Mode | Batch Mode (R-Tree) | Speedup |
|-----------|-------------|---------------------|---------|
| 100 Barangays | 300 queries | 3 queries | 100x |
| 1000 Barangays | 3000 queries | 3 queries | 1000x |
| Spatial Checks | O(n²) | O(n log n) | 50-100x |

---

## 6. Key Optimizations

1. **Bulk Loading**: Single region-wide queries instead of per-barangay queries
2. **R-Tree Indexing**: O(n log n) spatial assignment vs O(n²) nested loops
3. **In-Memory Grouping**: Avoids repeated database trips
4. **Technology Deduplication**: HashSet for cross-source uniqueness
5. **Compact JSON**: No indentation for storage efficiency

---

## 7. Error Handling

```mermaid
flowchart TD
    A[Operation Start] --> B{Try Execute}
    B -->|Success| C[Return Results]
    B -->|Exception| D[Log Error with Context]
    D --> E{Batch Mode?}
    E -->|Yes| F[Return Empty Dictionary]
    E -->|No| G[Return null]
    F --> H[Continue Job]
    G --> H
```

- All exceptions are logged with barangay context
- Single mode returns null on error
- Batch mode returns empty dictionary (partial results preserved)
- Errors don't crash the entire job

---

## 8. Integration Points

### Called By
- `ProcessConnectivityAssessmentJob.GenerateNewAssessment()`
- Regional hierarchical assessment processing

### Stores Data In
- `ConnectivityAssessment.WirelessCoverageJson` field

### Dependencies
- `ApplicationDbContext` for database access
- `ILogger` for diagnostics
- NetTopologySuite for spatial operations
- R-Tree index from NTS

---

## 9. Configuration Constants

```csharp
// None - all configuration is dynamic based on data
// Batch size determined by barangay count in ProcessConnectivityAssessmentJob
```

---

## 10. Monitoring & Logging

### Key Metrics Logged
- Cellular load time and count
- Validation load time and count
- Speedtest load time and count
- R-Tree build time
- Spatial assignment counts and rates
- Per-barangay processing time
- Total processing time

### Log Levels
- **Information**: Normal operations, performance metrics
- **Warning**: Missing boundaries, no data scenarios
- **Error**: Exceptions with full stack trace and context
