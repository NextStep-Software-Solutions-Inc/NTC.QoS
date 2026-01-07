# Dynamic Rubric System - Analysis and Implementation Plan

**Document Version:** 1.0  
**Date:** January 7, 2026  
**Status:** Analysis & Planning Phase  
**Branch:** feature/dynamic-rubrics

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Problem Statement](#problem-statement)
4. [Proposed Solution Architecture](#proposed-solution-architecture)
5. [Database Design](#database-design)
6. [API Design](#api-design)
7. [Scoring Engine Refactoring](#scoring-engine-refactoring)
8. [Migration Strategy](#migration-strategy)
9. [Testing Strategy](#testing-strategy)
10. [Implementation Phases](#implementation-phases)
11. [Risk Assessment](#risk-assessment)
12. [Recommendations](#recommendations)

---

## Executive Summary

### Current Situation
The CMTS (Connectivity Maturity and Technology Score) rubric is **hardcoded** throughout the application in multiple locations:
- Controller endpoints (`AnalyticsController.GetConnectivityRubric()`)
- Background job scoring logic (`ProcessConnectivityAssessmentJob`)
- Handler methods (`GenerateConnectivityAssessmentHandler`)
- Database enum (`ConnectivityStatusEnum`)

### Goal
Transform the rubric system to be **dynamic and user-configurable** while maintaining:
- **Data integrity** for historical assessments
- **Backward compatibility** with existing assessments
- **Performance** for background job processing
- **Flexibility** for different regions/contexts

### Strategic Approach
Implement a versioned, database-driven rubric configuration system that allows administrators to:
1. Create and manage custom rubric definitions
2. Configure scoring thresholds and weights
3. Version rubrics for historical tracking
4. Apply different rubrics per region/context
5. Maintain assessment reproducibility

---

## Current State Analysis

### 1. Hardcoded Rubric Locations

#### **Location 1: API Controller** (`AnalyticsController.cs`, Lines 193-294)
```csharp
[HttpGet("connectivity-rubric")]
public ActionResult<ApiResponse<ConnectivityRubricDto>> GetConnectivityRubric()
{
    var rubric = new ConnectivityRubricDto
    {
        ConnectivityStatuses = new List<ConnectivityStatusDto>
        {
            new ConnectivityStatusDto
            {
                Status = "Served",
                ScoreRange = "11-15 points", // HARDCODED
                Description = "Fully developed...",
                // ...
            },
            // Underserved, Unserved definitions...
        },
        Criteria = new List<RubricCriterionDto>
        {
            new RubricCriterionDto
            {
                Name = "Fiber Coverage",
                Weight = "0-3 points", // HARDCODED
                Served = "Fiber optic cables...",
                // ...
            },
            // 4 more criteria...
        }
    };
}
```

**Issues:**
- Rubric definition lives in code, not database
- Changes require code deployment
- No versioning or historical tracking
- Single rubric for all contexts

---

#### **Location 2: Scoring Logic** (`ProcessConnectivityAssessmentJob.cs`, Lines 2815-3200)

**Five scoring methods with hardcoded thresholds:**

```csharp
// 1. Fiber Coverage Score
private static int CalculateFiberCoverageScore(...)
{
    if (infrastructure.FiberNAPCount == 0) return 1; // HARDCODED: Unserved
    if (majorTelcoCount >= 2) return 3;              // HARDCODED: Served (≥2 major telcos)
    if (majorTelcoCount == 1) return 2;              // HARDCODED: Underserved (1 telco)
    return 1;
}

// 2. Internet Access Score
private static int CalculateInternetAccessScore(...)
{
    if (medianDownloadSpeed >= 25 && !isUnreliable) return 3; // HARDCODED: 25 Mbps threshold
    if (medianDownloadSpeed >= 10 && medianDownloadSpeed < 25) return 2; // HARDCODED: 10-25 Mbps
    return 1;
}

// 3. Technology Type Score
private static int CalculateTechnologyTypeScore(...)
{
    if (hasFiberInfra) return 3;                     // HARDCODED: Fiber = Served
    if (tech.Count5G > 0) return 3;                  // HARDCODED: 5G = Served
    if (tech.Count4G > 0 && !tech.IsLegacyDominated) return 3; // HARDCODED: 4G logic
    if (tech.IsLegacyDominated) return 2;            // HARDCODED: Legacy dominated
    return 1;
}

// 4. Quality of Service Score
private static int CalculateQualityOfServiceScore(...)
{
    if (latency > 100 || signalStrength <= 2.0 || ...) return 1; // HARDCODED: 100ms, signal 2.0
    if (hasLowLatency && hasGoodSignal && hasConsistentSpeeds) return 3; // HARDCODED: 50ms latency
    if (hasModerateLatency || hasModerateSignal || ...) return 2; // HARDCODED: 50-100ms
    return 1;
}

// 5. Coverage Availability Score
private static int CalculateCoverageAvailabilityScore(...)
{
    if (estimatedCoveragePercent >= 100) return 3;   // HARDCODED: 100% threshold
    else if (estimatedCoveragePercent >= 40) return 2; // HARDCODED: 40-99% threshold
    else return 1;                                   // HARDCODED: <40%
}
```

**Issues:**
- **30+ hardcoded thresholds** across 5 scoring methods
- No flexibility for regional variations (e.g., rural vs urban standards)
- Cannot adjust scoring without code changes
- Difficult to A/B test rubric variations

---

#### **Location 3: Status Determination** (`ProcessConnectivityAssessmentJob.cs`, Line ~2800)

```csharp
private static ConnectivityStatusEnum DetermineConnectivityStatus(int totalScore)
{
    return totalScore switch
    {
        >= 11 => ConnectivityStatusEnum.Served,      // HARDCODED: 11-15 = Served
        >= 6 => ConnectivityStatusEnum.Underserved,  // HARDCODED: 6-10 = Underserved
        _ => ConnectivityStatusEnum.Unserved         // HARDCODED: 0-5 = Unserved
    };
}
```

**Issues:**
- Fixed score ranges cannot be customized
- Enum-based status limits extensibility
- No support for additional status levels (e.g., "Marginally Served", "Critically Unserved")

---

#### **Location 4: Domain Enum** (`ConnectivityStatusEnum.cs`)

```csharp
public enum ConnectivityStatusEnum
{
    Served = 3,       // HARDCODED: 11-15 points
    Underserved = 2,  // HARDCODED: 6-10 points
    Unserved = 1      // HARDCODED: ≤5 points
}
```

**Issues:**
- Enum values are immutable
- Cannot add custom statuses without migration
- Integer values (1-3) limit flexibility

---

### 2. Data Dependencies

**Current Assessment Entity** (`ConnectivityAssessment.cs`):
```csharp
public class ConnectivityAssessment : BaseEntity
{
    // Geography
    public Guid LocationId { get; set; }
    public LocationTypeEnum LocationType { get; set; }
    
    // CMTS Results - NO REFERENCE TO RUBRIC VERSION
    public ConnectivityStatusEnum ConnectivityStatus { get; set; }
    public int TotalScore { get; set; } // 0-15
    
    // Individual scores (0-3 each) - NO REFERENCE TO RUBRIC CRITERIA
    public int FiberCoverageScore { get; set; }
    public int InternetAccessScore { get; set; }
    public int TechnologyTypeScore { get; set; }
    public int QualityOfServiceScore { get; set; }
    public int CoverageAvailabilityScore { get; set; }
    
    public DateTime AssessmentDate { get; set; }
    
    // Infrastructure data
    public int CellTowerCount { get; set; }
    public int FiberNAPCount { get; set; }
    // ... 40+ more properties
}
```

**Critical Missing Fields:**
- `RubricConfigurationId` - which rubric version was used?
- `RubricVersion` - version number for tracking changes
- Link to criterion definitions used

---

### 3. Current Workflow

```
┌─────────────────────────────────────────────────────────────┐
│  Current Static Rubric Workflow                             │
└─────────────────────────────────────────────────────────────┘

1. API Request: Start Assessment Job
   ↓
2. ProcessConnectivityAssessmentJob.ExecuteAsync()
   ↓
3. For each location:
   │
   ├─→ Calculate Infrastructure Stats (database queries)
   ├─→ Calculate Speed Test Stats (database queries)
   ├─→ Calculate CMTS Scores (HARDCODED LOGIC):
   │   ├─→ CalculateFiberCoverageScore()       [Uses hardcoded thresholds]
   │   ├─→ CalculateInternetAccessScore()      [Uses hardcoded thresholds]
   │   ├─→ CalculateTechnologyTypeScore()      [Uses hardcoded thresholds]
   │   ├─→ CalculateQualityOfServiceScore()    [Uses hardcoded thresholds]
   │   └─→ CalculateCoverageAvailabilityScore()[Uses hardcoded thresholds]
   │
   ├─→ Sum Total Score (0-15)
   ├─→ DetermineConnectivityStatus()            [Uses hardcoded ranges]
   └─→ Save to Database (no rubric reference)
```

**Problems:**
- No injection point for dynamic rubric configuration
- Scoring methods are static (no dependency injection)
- No rubric version tracking in assessment records

---

## Problem Statement

### Business Problems

1. **Inflexibility:** Cannot adjust scoring criteria without code deployment
2. **Regional Variation:** Cannot apply different standards for urban vs rural areas
3. **Policy Changes:** Government standard updates require code changes
4. **Experimentation:** Cannot A/B test different rubric configurations
5. **Audit Trail:** Historical assessments lack rubric version context
6. **Reproducibility:** Cannot recreate past assessments with original rubric

### Technical Problems

1. **Code Coupling:** Scoring logic tightly coupled to hardcoded values
2. **No Versioning:** Assessments don't reference which rubric version was used
3. **Static Methods:** Scoring methods cannot be unit tested with different configs
4. **Data Integrity:** Changing code can invalidate historical assessments
5. **Scalability:** Adding new criteria requires code changes in multiple files

---

## Proposed Solution Architecture

### Design Principles

1. **Separation of Concerns:** Separate rubric configuration from scoring engine
2. **Versioning:** All rubric changes create new versions, never modify existing
3. **Backward Compatibility:** Historical assessments remain valid and reproducible
4. **Performance:** Cache rubric configs to avoid database overhead
5. **Flexibility:** Support multiple active rubrics for different contexts
6. **Testability:** Scoring logic testable with different configurations

---

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│  Dynamic Rubric System Architecture                                  │
└──────────────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│   Admin UI/API      │  ← Manage rubric configurations
└──────────┬──────────┘
           │
           ▼
┌─────────────────────────────────────────────────────┐
│  Rubric Management Service                          │
│  ------------------------------------------------   │
│  • Create/Update/Version rubrics                    │
│  • Validate configurations                          │
│  • Set default rubrics                              │
│  • Archive old versions                             │
└──────────┬──────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────┐
│  Database: Rubric Configuration Tables              │
│  ------------------------------------------------   │
│  • RubricConfiguration (master)                     │
│  • RubricCriterion (individual criteria)            │
│  • RubricThreshold (scoring thresholds)             │
│  • RubricStatusRange (status definitions)           │
└──────────┬──────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────┐
│  Rubric Repository & Cache                          │
│  ------------------------------------------------   │
│  • Load rubric by ID or version                     │
│  • Cache active rubrics (MemoryCache)               │
│  • Invalidate cache on updates                      │
└──────────┬──────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────┐
│  Dynamic Scoring Engine                             │
│  ------------------------------------------------   │
│  • Inject rubric configuration                      │
│  • Apply criteria dynamically                       │
│  • Calculate scores using config thresholds         │
│  • Determine status using config ranges             │
└──────────┬──────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────┐
│  Enhanced ConnectivityAssessment Entity             │
│  ------------------------------------------------   │
│  • RubricConfigurationId (FK)                       │
│  • RubricVersion                                    │
│  • All scores + infrastructure data                 │
└─────────────────────────────────────────────────────┘
```

---

## Database Design

### Entity Relationship Diagram

```
┌──────────────────────────────────┐
│ RubricConfiguration              │
├──────────────────────────────────┤
│ Id (PK)                          │
│ Name                             │
│ Description                      │
│ Version                          │ ← Semantic versioning (1.0.0)
│ IsActive                         │
│ IsDefault                        │
│ EffectiveFrom                    │
│ EffectiveTo                      │
│ ApplicableLocationType           │ ← Region/Province/Municipality/Barangay
│ CreatedBy, CreatedAt             │
│ UpdatedBy, UpdatedAt             │
└──────────┬───────────────────────┘
           │ 1
           │
           │ N
┌──────────▼───────────────────────┐
│ RubricCriterion                  │
├──────────────────────────────────┤
│ Id (PK)                          │
│ RubricConfigurationId (FK)       │
│ CriterionType (enum)             │ ← FiberCoverage, InternetAccess, etc.
│ Name                             │
│ Description                      │
│ MaxScore                         │ ← Usually 3
│ Weight                           │ ← For future weighted scoring
│ SortOrder                        │
│ IsEnabled                        │
└──────────┬───────────────────────┘
           │ 1
           │
           │ N
┌──────────▼───────────────────────┐
│ RubricThreshold                  │
├──────────────────────────────────┤
│ Id (PK)                          │
│ RubricCriterionId (FK)           │
│ ScoreValue                       │ ← 3=Served, 2=Underserved, 1=Unserved
│ Operator (enum)                  │ ← GreaterThan, LessThan, Between, etc.
│ PrimaryValue                     │ ← Numeric threshold (e.g., 25 Mbps)
│ SecondaryValue                   │ ← For range thresholds (e.g., 10-25)
│ MetricType                       │ ← MedianSpeed, LatencyMs, CoveragePercent
│ ConditionLogic (JSON)            │ ← Complex AND/OR conditions
│ Description                      │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│ RubricStatusRange                │
├──────────────────────────────────┤
│ Id (PK)                          │
│ RubricConfigurationId (FK)       │
│ StatusName                       │ ← "Served", "Underserved", "Unserved"
│ MinScore                         │ ← 11 for Served
│ MaxScore                         │ ← 15 for Served
│ StatusValue (enum)               │ ← Maps to ConnectivityStatusEnum
│ DisplayColor                     │ ← For UI rendering
│ Description                      │
│ SortOrder                        │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│ ConnectivityAssessment (Enhanced)│
├──────────────────────────────────┤
│ ... (existing fields)            │
│ RubricConfigurationId (FK) NEW   │ ← Links to rubric used
│ RubricVersion (string) NEW       │ ← Snapshot for reproducibility
│ ... (scores, infrastructure)     │
└──────────────────────────────────┘
```

---

### Table Definitions

#### **1. RubricConfiguration**

Master table for rubric versions.

```sql
CREATE TABLE "RubricConfiguration" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" varchar(200) NOT NULL,
    "Description" text,
    "Version" varchar(20) NOT NULL, -- Semantic versioning (e.g., "1.0.0")
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "ApplicableLocationType" int NULL, -- NULL = all, or specific LocationTypeEnum
    "ConfigurationJson" jsonb NULL, -- Full rubric snapshot for reproducibility
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" text NOT NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "DeletedBy" text NULL,
    
    CONSTRAINT "UQ_RubricConfiguration_Version" UNIQUE ("Version", "IsDeleted")
);

CREATE INDEX "IX_RubricConfiguration_IsActive" ON "RubricConfiguration" ("IsActive") WHERE "IsDeleted" = false;
CREATE INDEX "IX_RubricConfiguration_IsDefault" ON "RubricConfiguration" ("IsDefault") WHERE "IsDeleted" = false;
CREATE INDEX "IX_RubricConfiguration_EffectiveFrom" ON "RubricConfiguration" ("EffectiveFrom");
```

**Key Features:**
- **Versioning:** Semantic versioning (Major.Minor.Patch)
- **Effective Dates:** Time-based rubric activation
- **Soft Delete:** Preserve historical rubric configurations
- **Location Scoping:** Apply different rubrics per location type
- **JSON Snapshot:** Full configuration stored for reproducibility

---

#### **2. RubricCriterion**

Individual scoring criteria (e.g., Fiber Coverage, Internet Access).

```sql
CREATE TABLE "RubricCriterion" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "RubricConfigurationId" uuid NOT NULL,
    "CriterionType" int NOT NULL, -- Enum: FiberCoverage=1, InternetAccess=2, etc.
    "Name" varchar(200) NOT NULL,
    "Description" text,
    "MaxScore" int NOT NULL DEFAULT 3,
    "Weight" decimal(5,2) NOT NULL DEFAULT 1.0, -- For weighted scoring
    "SortOrder" int NOT NULL,
    "IsEnabled" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" text NOT NULL,
    
    CONSTRAINT "FK_RubricCriterion_RubricConfiguration" 
        FOREIGN KEY ("RubricConfigurationId") 
        REFERENCES "RubricConfiguration"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RubricCriterion_RubricConfigurationId" ON "RubricCriterion" ("RubricConfigurationId");
CREATE INDEX "IX_RubricCriterion_CriterionType" ON "RubricCriterion" ("CriterionType");
```

**Key Features:**
- **Extensibility:** Add new criteria without code changes
- **Weighting:** Support future weighted scoring models
- **Ordering:** Control criterion display order
- **Enable/Disable:** Temporarily disable criteria without deletion

---

#### **3. RubricThreshold**

Scoring thresholds for each criterion (e.g., ≥25 Mbps = 3 points).

```sql
CREATE TABLE "RubricThreshold" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "RubricCriterionId" uuid NOT NULL,
    "ScoreValue" int NOT NULL, -- 3=Served, 2=Underserved, 1=Unserved, 0=NoData
    "Operator" int NOT NULL, -- Enum: GreaterThanOrEqual=1, LessThan=2, Between=3, etc.
    "PrimaryValue" decimal(18,4) NOT NULL, -- Main threshold value
    "SecondaryValue" decimal(18,4) NULL, -- For range operators (Between, NotBetween)
    "MetricType" varchar(100) NOT NULL, -- "MedianDownloadSpeedMbps", "LatencyMs", etc.
    "ConditionLogic" jsonb NULL, -- Complex multi-condition logic
    "Description" text,
    "SortOrder" int NOT NULL, -- Evaluation order (highest score first)
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" text NOT NULL,
    
    CONSTRAINT "FK_RubricThreshold_RubricCriterion" 
        FOREIGN KEY ("RubricCriterionId") 
        REFERENCES "RubricCriterion"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RubricThreshold_RubricCriterionId" ON "RubricThreshold" ("RubricCriterionId");
CREATE INDEX "IX_RubricThreshold_ScoreValue" ON "RubricThreshold" ("ScoreValue");
```

**Key Features:**
- **Flexible Operators:** ≥, ≤, Between, NotBetween, Equals, Contains
- **Multi-Metric:** Support composite conditions (speed AND latency)
- **Order Control:** Evaluate highest scores first (score 3 → 2 → 1)
- **JSON Logic:** Store complex conditions for edge cases

**Example Thresholds (Internet Access):**

| Score | Operator          | PrimaryValue | SecondaryValue | MetricType              | Description              |
|-------|-------------------|--------------|----------------|-------------------------|--------------------------|
| 3     | GreaterThanOrEqual| 25.0         | NULL           | MedianDownloadSpeedMbps | Served: ≥25 Mbps         |
| 2     | Between           | 10.0         | 24.99          | MedianDownloadSpeedMbps | Underserved: 10-25 Mbps  |
| 1     | LessThan          | 10.0         | NULL           | MedianDownloadSpeedMbps | Unserved: <10 Mbps       |

---

#### **4. RubricStatusRange**

Defines connectivity status based on total score.

```sql
CREATE TABLE "RubricStatusRange" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "RubricConfigurationId" uuid NOT NULL,
    "StatusName" varchar(100) NOT NULL, -- "Served", "Underserved", "Unserved"
    "MinScore" int NOT NULL,
    "MaxScore" int NOT NULL,
    "StatusValue" int NOT NULL, -- Maps to ConnectivityStatusEnum
    "DisplayColor" varchar(20) NULL, -- Hex color for UI
    "Description" text,
    "SortOrder" int NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    
    CONSTRAINT "FK_RubricStatusRange_RubricConfiguration" 
        FOREIGN KEY ("RubricConfigurationId") 
        REFERENCES "RubricConfiguration"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_RubricStatusRange_ScoreRange" CHECK ("MinScore" <= "MaxScore")
);

CREATE INDEX "IX_RubricStatusRange_RubricConfigurationId" ON "RubricStatusRange" ("RubricConfigurationId");
```

**Example Status Ranges:**

| StatusName   | MinScore | MaxScore | StatusValue | DisplayColor |
|--------------|----------|----------|-------------|--------------|
| Served       | 11       | 15       | 3           | #28A745      |
| Underserved  | 6        | 10       | 2           | #FFC107      |
| Unserved     | 0        | 5        | 1           | #DC3545      |

---

### Migration Strategy (Database)

**Phase 1: Add New Tables**
- Create `RubricConfiguration`, `RubricCriterion`, `RubricThreshold`, `RubricStatusRange`
- Seed default CMTS rubric (v1.0.0) matching current hardcoded logic

**Phase 2: Enhance ConnectivityAssessment**
- Add `RubricConfigurationId` (nullable initially for backward compatibility)
- Add `RubricVersion` (string, nullable)
- Create index on `RubricConfigurationId`

**Phase 3: Backfill Historical Data**
- Update existing assessments to reference default rubric (v1.0.0)
- Set `RubricConfigurationId` to seeded default rubric ID
- Set `RubricVersion = "1.0.0"` for all existing assessments

**Phase 4: Enforce Foreign Keys**
- Alter `RubricConfigurationId` to NOT NULL (after backfill)
- Add foreign key constraint to `RubricConfiguration`

---

## API Design

### 1. Rubric Management Endpoints

#### **GET /api/rubrics**
List all rubric configurations.

**Query Parameters:**
- `isActive` (bool): Filter by active status
- `locationType` (LocationTypeEnum): Filter by applicable location type
- `pageNumber` (int): Pagination
- `pageSize` (int): Pagination

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "name": "CMTS Standard Rubric",
        "version": "1.0.0",
        "description": "Official CMTS Broadband Mapping Rubric",
        "isActive": true,
        "isDefault": true,
        "effectiveFrom": "2024-01-01T00:00:00Z",
        "effectiveTo": null,
        "criteriaCount": 5,
        "createdAt": "2024-01-01T00:00:00Z",
        "createdBy": "System"
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 10
  }
}
```

---

#### **GET /api/rubrics/{id}**
Get detailed rubric configuration including criteria and thresholds.

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "id": "uuid",
    "name": "CMTS Standard Rubric",
    "version": "1.0.0",
    "description": "Official CMTS Broadband Mapping Rubric",
    "isActive": true,
    "isDefault": true,
    "effectiveFrom": "2024-01-01T00:00:00Z",
    "criteria": [
      {
        "id": "uuid",
        "criterionType": "FiberCoverage",
        "name": "Fiber Coverage",
        "description": "Presence of fiber optic infrastructure",
        "maxScore": 3,
        "weight": 1.0,
        "sortOrder": 1,
        "isEnabled": true,
        "thresholds": [
          {
            "id": "uuid",
            "scoreValue": 3,
            "operator": "GreaterThanOrEqual",
            "primaryValue": 2.0,
            "metricType": "MajorTelcoCount",
            "description": "Served: ≥2 major telcos with fiber"
          },
          {
            "id": "uuid",
            "scoreValue": 2,
            "operator": "Equals",
            "primaryValue": 1.0,
            "metricType": "MajorTelcoCount",
            "description": "Underserved: 1 major telco with fiber"
          },
          {
            "id": "uuid",
            "scoreValue": 1,
            "operator": "Equals",
            "primaryValue": 0.0,
            "metricType": "FiberNAPCount",
            "description": "Unserved: No fiber infrastructure"
          }
        ]
      }
      // ... 4 more criteria
    ],
    "statusRanges": [
      {
        "id": "uuid",
        "statusName": "Served",
        "minScore": 11,
        "maxScore": 15,
        "statusValue": 3,
        "displayColor": "#28A745",
        "description": "Fully developed, meets most technical benchmarks"
      },
      {
        "id": "uuid",
        "statusName": "Underserved",
        "minScore": 6,
        "maxScore": 10,
        "statusValue": 2,
        "displayColor": "#FFC107",
        "description": "Basic infrastructure but lacks quality"
      },
      {
        "id": "uuid",
        "statusName": "Unserved",
        "minScore": 0,
        "maxScore": 5,
        "statusValue": 1,
        "displayColor": "#DC3545",
        "description": "Virtually no broadband access"
      }
    ]
  }
}
```

---

#### **POST /api/rubrics**
Create a new rubric configuration.

**Request Body:**
```json
{
  "name": "Rural Areas Rubric",
  "description": "Modified thresholds for rural connectivity assessment",
  "version": "1.1.0",
  "basedOnRubricId": "uuid", // Optional: clone existing rubric
  "effectiveFrom": "2026-02-01T00:00:00Z",
  "applicableLocationType": 4, // Barangay
  "criteria": [
    {
      "criterionType": "FiberCoverage",
      "name": "Fiber Coverage",
      "maxScore": 3,
      "weight": 1.0,
      "sortOrder": 1,
      "thresholds": [
        {
          "scoreValue": 3,
          "operator": "GreaterThanOrEqual",
          "primaryValue": 1.0, // Lower threshold for rural areas
          "metricType": "MajorTelcoCount",
          "description": "Served: ≥1 major telco (rural standard)"
        }
        // ... more thresholds
      ]
    }
    // ... more criteria
  ],
  "statusRanges": [
    {
      "statusName": "Served",
      "minScore": 9, // Lower bar for rural areas
      "maxScore": 15,
      "statusValue": 3
    }
    // ... more ranges
  ]
}
```

**Response:** 201 Created with rubric details

---

#### **PUT /api/rubrics/{id}**
Update rubric configuration (creates new version).

**Behavior:**
- Always creates a new version (immutable versioning)
- Increments version number (e.g., 1.0.0 → 1.1.0)
- Preserves old version for historical assessments
- Sets `EffectiveTo` on old version

---

#### **DELETE /api/rubrics/{id}**
Soft delete a rubric configuration.

**Validation:**
- Cannot delete if referenced by assessments (unless force flag)
- Cannot delete default rubric
- Sets `IsDeleted = true` and `DeletedAt`

---

#### **POST /api/rubrics/{id}/clone**
Clone an existing rubric for modification.

**Response:** New rubric with incremented version

---

#### **POST /api/rubrics/{id}/set-default**
Set a rubric as the default for new assessments.

---

### 2. Enhanced Assessment Endpoints

#### **POST /api/analytics/generate-connectivity-assessment**
Enhanced to accept `rubricConfigurationId`.

**Request Body (Enhanced):**
```json
{
  "locationIds": ["uuid1", "uuid2"],
  "locationType": 4,
  "rubricConfigurationId": "uuid", // NEW: Optional, defaults to system default
  "forceRecalculate": true,
  "startDate": "2024-01-01",
  "endDate": "2024-12-31"
}
```

**Behavior:**
- If `rubricConfigurationId` is null, use default rubric
- Validate rubric is active and applicable to location type
- Store `RubricConfigurationId` and `RubricVersion` in assessment

---

#### **GET /api/analytics/connectivity-analysis**
Returns rubric version used for each assessment.

**Response (Enhanced):**
```json
{
  "succeeded": true,
  "data": {
    "locations": [
      {
        "locationId": "uuid",
        "locationName": "Barangay Example",
        "totalScore": 12,
        "connectivityStatus": "Served",
        "rubricVersion": "1.0.0", // NEW: Show which rubric was used
        "rubricName": "CMTS Standard Rubric", // NEW
        "assessmentDate": "2026-01-07T10:00:00Z",
        "cmtsScores": {
          "fiberCoverage": 3,
          "internetAccess": 2,
          "technologyType": 3,
          "qualityOfService": 2,
          "coverageAvailability": 2
        }
      }
    ]
  }
}
```

---

## Scoring Engine Refactoring

### Current vs. Proposed Architecture

#### **Current (Static Methods)**

```csharp
public class ProcessConnectivityAssessmentJob
{
    // Static method - cannot inject configuration
    private static int CalculateInternetAccessScore(SpeedStatsDto? speedStats)
    {
        if (speedStats == null) return 1;
        
        // HARDCODED THRESHOLD
        if (speedStats.MedianDownloadSpeedMbps >= 25 && !isUnreliable)
            return 3;
        
        // More hardcoded logic...
    }
}
```

**Problems:**
- No dependency injection
- Cannot pass rubric configuration
- Static = untestable with different configs

---

#### **Proposed (Dynamic Scoring Service)**

```csharp
// New service interface
public interface IRubricScoringService
{
    Task<CMTSScoresDto> CalculateCMTSScoresAsync(
        RubricConfiguration rubric,
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats,
        double areaKm2,
        int? population);
        
    Task<int> CalculateCriterionScoreAsync(
        RubricCriterion criterion,
        Dictionary<string, object> metrics);
        
    ConnectivityStatusEnum DetermineStatus(
        RubricConfiguration rubric,
        int totalScore);
}

// Implementation
public class RubricScoringService : IRubricScoringService
{
    private readonly ILogger<RubricScoringService> _logger;
    
    public async Task<CMTSScoresDto> CalculateCMTSScoresAsync(
        RubricConfiguration rubric,
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats,
        double areaKm2,
        int? population)
    {
        var scores = new CMTSScoresDto();
        var metrics = BuildMetricsDictionary(infrastructure, speedStats, areaKm2, population);
        
        // Dynamically calculate each criterion
        foreach (var criterion in rubric.Criteria.Where(c => c.IsEnabled))
        {
            var score = await CalculateCriterionScoreAsync(criterion, metrics);
            
            // Map to DTO property
            switch (criterion.CriterionType)
            {
                case CriterionTypeEnum.FiberCoverage:
                    scores.FiberCoverage = score;
                    break;
                case CriterionTypeEnum.InternetAccess:
                    scores.InternetAccess = score;
                    break;
                // ... other criteria
            }
        }
        
        return scores;
    }
    
    public async Task<int> CalculateCriterionScoreAsync(
        RubricCriterion criterion,
        Dictionary<string, object> metrics)
    {
        // Evaluate thresholds in order (highest score first)
        var thresholds = criterion.Thresholds.OrderByDescending(t => t.ScoreValue);
        
        foreach (var threshold in thresholds)
        {
            if (await EvaluateThresholdAsync(threshold, metrics))
            {
                return threshold.ScoreValue; // Return first match
            }
        }
        
        return 0; // No threshold met
    }
    
    private async Task<bool> EvaluateThresholdAsync(
        RubricThreshold threshold,
        Dictionary<string, object> metrics)
    {
        // Get metric value
        if (!metrics.TryGetValue(threshold.MetricType, out var metricValue))
        {
            return false; // Metric not available
        }
        
        var value = Convert.ToDecimal(metricValue);
        
        // Evaluate based on operator
        return threshold.Operator switch
        {
            ThresholdOperatorEnum.GreaterThanOrEqual => value >= threshold.PrimaryValue,
            ThresholdOperatorEnum.LessThan => value < threshold.PrimaryValue,
            ThresholdOperatorEnum.Between => 
                value >= threshold.PrimaryValue && value <= threshold.SecondaryValue,
            ThresholdOperatorEnum.Equals => 
                Math.Abs(value - threshold.PrimaryValue) < 0.0001m,
            // ... other operators
            _ => false
        };
    }
    
    public ConnectivityStatusEnum DetermineStatus(
        RubricConfiguration rubric,
        int totalScore)
    {
        // Find status range that contains the score
        var statusRange = rubric.StatusRanges
            .FirstOrDefault(r => totalScore >= r.MinScore && totalScore <= r.MaxScore);
        
        return statusRange?.StatusValue ?? ConnectivityStatusEnum.Unserved;
    }
    
    private Dictionary<string, object> BuildMetricsDictionary(
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats,
        double areaKm2,
        int? population)
    {
        return new Dictionary<string, object>
        {
            // Infrastructure metrics
            ["FiberNAPCount"] = infrastructure.FiberNAPCount,
            ["CellTowerCount"] = infrastructure.CellTowerCount,
            ["MajorTelcoCount"] = CountMajorTelcos(infrastructure.ProviderBreakdown),
            
            // Speed metrics
            ["MedianDownloadSpeedMbps"] = speedStats?.MedianDownloadSpeedMbps ?? 0m,
            ["AverageLatencyMs"] = speedStats?.AverageLatencyMs ?? decimal.MaxValue,
            ["AverageSignalStrength"] = speedStats?.AverageSignalStrength ?? 0m,
            
            // Technology metrics
            ["Has5G"] = infrastructure.TechnologyBandBreakdown?.Count5G > 0,
            ["Has4G"] = infrastructure.TechnologyBandBreakdown?.Count4G > 0,
            ["IsLegacyDominated"] = infrastructure.TechnologyBandBreakdown?.IsLegacyDominated ?? true,
            
            // Coverage metrics
            ["EstimatedCoveragePercent"] = CalculateCoveragePercent(infrastructure, population, areaKm2),
            
            // Context
            ["PopulationDensity"] = population.HasValue && areaKm2 > 0 ? population.Value / areaKm2 : 0
        };
    }
}
```

---

### Refactored Background Job

```csharp
public class ProcessConnectivityAssessmentJob : IProcessConnectivityAssessmentJob
{
    private readonly IRubricRepository _rubricRepository;
    private readonly IRubricScoringService _scoringService;
    // ... other dependencies
    
    public async Task ExecuteAsync(
        ConnectivityAssessmentJobRequestDto jobRequest,
        CancellationToken cancellationToken)
    {
        // 1. Load rubric configuration
        var rubric = await LoadRubricConfigurationAsync(
            jobRequest.RubricConfigurationId,
            cancellationToken);
        
        // 2. Process locations with dynamic scoring
        foreach (var location in locations)
        {
            // Calculate infrastructure and speed stats (unchanged)
            var infrastructure = await CalculateInfrastructureStatsAsync(...);
            var speedStats = await CalculateSpeedStatsAsync(...);
            
            // 3. Calculate scores using injected rubric
            var cmtsScores = await _scoringService.CalculateCMTSScoresAsync(
                rubric,
                infrastructure,
                speedStats,
                areaKm2,
                population);
            
            var totalScore = cmtsScores.FiberCoverage + 
                           cmtsScores.InternetAccess +
                           cmtsScores.TechnologyType +
                           cmtsScores.QualityOfService +
                           cmtsScores.CoverageAvailability;
            
            // 4. Determine status using rubric
            var status = _scoringService.DetermineStatus(rubric, totalScore);
            
            // 5. Save with rubric reference
            var assessment = new ConnectivityAssessment
            {
                LocationId = locationId,
                LocationType = locationType,
                RubricConfigurationId = rubric.Id, // NEW
                RubricVersion = rubric.Version,    // NEW
                TotalScore = totalScore,
                ConnectivityStatus = status,
                FiberCoverageScore = cmtsScores.FiberCoverage,
                // ... other scores
            };
            
            await _connectivityRepository.AddAsync(assessment);
        }
    }
    
    private async Task<RubricConfiguration> LoadRubricConfigurationAsync(
        Guid? rubricId,
        CancellationToken cancellationToken)
    {
        if (rubricId.HasValue)
        {
            // Load specific rubric
            return await _rubricRepository.GetByIdAsync(rubricId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Rubric {rubricId} not found");
        }
        
        // Load default rubric
        return await _rubricRepository.GetDefaultRubricAsync(cancellationToken)
            ?? throw new InvalidOperationException("No default rubric configured");
    }
}
```

---

## Testing Strategy

### 1. Unit Tests

#### **Test: Dynamic Scoring with Different Thresholds**

```csharp
public class RubricScoringServiceTests
{
    [Fact]
    public async Task CalculateCriterionScore_InternetAccess_Served_ReturnsThree()
    {
        // Arrange
        var rubric = CreateTestRubric(internetAccessThreshold: 25m); // 25 Mbps for Served
        var metrics = new Dictionary<string, object>
        {
            ["MedianDownloadSpeedMbps"] = 30m // Above threshold
        };
        
        var service = new RubricScoringService(Mock.Of<ILogger<RubricScoringService>>());
        var criterion = rubric.Criteria.First(c => c.CriterionType == CriterionTypeEnum.InternetAccess);
        
        // Act
        var score = await service.CalculateCriterionScoreAsync(criterion, metrics);
        
        // Assert
        Assert.Equal(3, score); // Served
    }
    
    [Fact]
    public async Task CalculateCriterionScore_InternetAccess_RuralRubric_ReturnsThree()
    {
        // Arrange - Rural rubric with lower threshold
        var ruralRubric = CreateTestRubric(internetAccessThreshold: 10m); // 10 Mbps for rural
        var metrics = new Dictionary<string, object>
        {
            ["MedianDownloadSpeedMbps"] = 15m // Would be Underserved in standard, Served in rural
        };
        
        var service = new RubricScoringService(Mock.Of<ILogger<RubricScoringService>>());
        var criterion = ruralRubric.Criteria.First(c => c.CriterionType == CriterionTypeEnum.InternetAccess);
        
        // Act
        var score = await service.CalculateCriterionScoreAsync(criterion, metrics);
        
        // Assert
        Assert.Equal(3, score); // Served under rural standard
    }
}
```

---

### 2. Integration Tests

#### **Test: End-to-End Assessment with Custom Rubric**

```csharp
public class ConnectivityAssessmentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GenerateAssessment_WithCustomRubric_UsesCorrectThresholds()
    {
        // Arrange - Create custom rubric
        var client = _factory.CreateClient();
        var customRubric = await CreateCustomRubricAsync(client, ruralThresholds: true);
        
        // Act - Generate assessment with custom rubric
        var request = new GenerateConnectivityAssessmentRequest
        {
            LocationIds = [TestBarangayId],
            LocationType = LocationTypeEnum.Barangay,
            RubricConfigurationId = customRubric.Id
        };
        
        var response = await client.PostAsJsonAsync("/api/analytics/generate-connectivity-assessment", request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<LocationConnectivityDto>>>();
        
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Single(result.Data);
        
        var assessment = result.Data.First();
        Assert.Equal(customRubric.Version, assessment.RubricVersion); // Verify rubric used
        Assert.InRange(assessment.TotalScore, 0, 15);
    }
}
```

---

### 3. Performance Tests

#### **Test: Cached Rubric Loading**

```csharp
[Fact]
public async Task LoadRubric_SubsequentCalls_UsesCache()
{
    // Arrange
    var repository = CreateRubricRepositoryWithCache();
    
    // Act - Load same rubric twice
    var stopwatch = Stopwatch.StartNew();
    var rubric1 = await repository.GetDefaultRubricAsync();
    var firstCallTime = stopwatch.ElapsedMilliseconds;
    
    stopwatch.Restart();
    var rubric2 = await repository.GetDefaultRubricAsync();
    var secondCallTime = stopwatch.ElapsedMilliseconds;
    
    // Assert
    Assert.Same(rubric1, rubric2); // Same instance from cache
    Assert.True(secondCallTime < firstCallTime / 10); // 10x faster from cache
}
```

---

## Implementation Phases

### **Phase 1: Foundation (Week 1-2)**

**Goals:**
- Database schema for rubric configuration
- Seed default CMTS rubric matching current logic
- Basic repository and caching layer

**Deliverables:**
1. EF Core entities: `RubricConfiguration`, `RubricCriterion`, `RubricThreshold`, `RubricStatusRange`
2. Database migration with new tables
3. Seed data: Default CMTS v1.0.0 rubric
4. Repository interfaces and implementations
5. Unit tests for repository layer

**Acceptance Criteria:**
- ✅ Database migration runs successfully
- ✅ Default rubric seeded with 5 criteria, 15+ thresholds
- ✅ Can query rubric by ID, version, and default status

---

### **Phase 2: Scoring Engine (Week 3-4)**

**Goals:**
- Implement dynamic scoring service
- Refactor background job to use rubric service
- Add rubric reference to assessments

**Deliverables:**
1. `IRubricScoringService` interface and implementation
2. Enhanced `ConnectivityAssessment` entity (add `RubricConfigurationId`, `RubricVersion`)
3. Database migration for assessment table changes
4. Backfill script for historical assessments
5. Refactored `ProcessConnectivityAssessmentJob` with dependency injection
6. Unit tests for scoring service (50+ test cases covering all thresholds)

**Acceptance Criteria:**
- ✅ Scoring service calculates same scores as hardcoded logic (parity tests)
- ✅ Background job successfully uses dynamic rubric
- ✅ All existing assessments backfilled with default rubric reference
- ✅ New assessments store rubric version

---

### **Phase 3: API & Management (Week 5-6)**

**Goals:**
- Rubric management API endpoints
- Admin UI for rubric configuration
- Versioning and cloning features

**Deliverables:**
1. `RubricsController` with CRUD endpoints
2. MediatR handlers for rubric operations
3. Validation logic for rubric configurations
4. Rubric cloning and versioning logic
5. Enhanced assessment endpoints (accept `rubricConfigurationId`)
6. API documentation (Swagger annotations)
7. Integration tests for all endpoints

**Acceptance Criteria:**
- ✅ Can create, update, view, delete rubrics via API
- ✅ Rubric updates create new versions (immutable)
- ✅ Can clone existing rubric with modifications
- ✅ Assessment API accepts custom rubric ID
- ✅ All endpoints have comprehensive tests

---

### **Phase 4: Advanced Features (Week 7-8)**

**Goals:**
- Location-type-specific rubrics
- Time-based rubric activation
- Complex threshold conditions (AND/OR logic)
- Performance optimization

**Deliverables:**
1. Location-type filtering for rubrics
2. Effective date validation and enforcement
3. JSON-based complex condition evaluator
4. Rubric configuration caching (MemoryCache with 1-hour expiration)
5. Performance benchmarks (ensure <10% overhead vs hardcoded)
6. Migration guide for administrators

**Acceptance Criteria:**
- ✅ Can apply different rubrics per location type
- ✅ Rubrics activate automatically on effective date
- ✅ Supports complex conditions (e.g., "Speed ≥25 AND Latency <50")
- ✅ Rubric loading adds <50ms overhead per assessment
- ✅ Documentation for creating custom rubrics

---

### **Phase 5: UI & Visualization (Week 9-10)**

**Goals:**
- Admin dashboard for rubric management
- Visual rubric editor
- Assessment comparison with different rubrics

**Deliverables:**
1. Rubric list/detail pages (Angular/React)
2. Visual threshold editor (drag-and-drop sliders)
3. Rubric comparison tool (side-by-side view)
4. Assessment re-calculation with different rubrics
5. Rubric preview (test scoring without saving)
6. User documentation and tutorials

**Acceptance Criteria:**
- ✅ Non-technical admins can create rubrics via UI
- ✅ Can visually adjust thresholds with immediate preview
- ✅ Can compare how 2+ rubrics score the same location
- ✅ Complete user guide with screenshots

---

## Risk Assessment

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Performance Degradation** | Medium | High | - Cache rubric configs (MemoryCache)<br>- Benchmark vs baseline (target <10% overhead)<br>- Optimize threshold evaluation (early exit) |
| **Breaking Changes** | Medium | High | - Phase migrations (nullable FK → required)<br>- Backfill historical data<br>- Keep hardcoded logic as fallback (Phase 1) |
| **Complexity Creep** | High | Medium | - Start with simple operators (≥, <, Between)<br>- Add complex logic (AND/OR) in Phase 4 only<br>- Strict validation on rubric creation |
| **Data Inconsistency** | Low | High | - Immutable versioning (never modify existing)<br>- Snapshot rubric in JSON (full reproducibility)<br>- Foreign key constraints |

---

### Business Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **User Confusion** | Medium | Medium | - Comprehensive documentation<br>- Default to standard CMTS rubric<br>- Warning when changing default rubric |
| **Regulatory Compliance** | Low | High | - Audit trail (who changed rubric, when)<br>- Cannot delete rubrics used in assessments<br>- Version history immutable |
| **Migration Errors** | Medium | High | - Test migrations on staging<br>- Backup production data before migration<br>- Rollback plan documented |

---

## Recommendations

### 1. **Adopt Phased Approach**
- **DO NOT** attempt all features at once
- Start with Phase 1-2 (database + scoring engine)
- Validate scoring parity before exposing to users
- Add management features (Phase 3) only after core is stable

---

### 2. **Maintain Backward Compatibility**
- Keep hardcoded scoring logic as fallback (config flag)
- Nullable `RubricConfigurationId` during migration
- Default to "CMTS Standard v1.0.0" if no rubric specified
- Provide migration tool to backfill historical assessments

---

### 3. **Implement Robust Caching**
- Cache rubric configurations (1-hour expiration)
- Invalidate cache on rubric updates
- Use distributed cache (Redis) for multi-server deployments
- Cache key: `rubric:{id}` and `rubric:default`

**Example:**
```csharp
public async Task<RubricConfiguration> GetRubricAsync(Guid id)
{
    var cacheKey = $"rubric:{id}";
    
    if (_cache.TryGetValue(cacheKey, out RubricConfiguration cachedRubric))
    {
        return cachedRubric;
    }
    
    var rubric = await _repository.GetByIdAsync(id);
    _cache.Set(cacheKey, rubric, TimeSpan.FromHours(1));
    
    return rubric;
}
```

---

### 4. **Enforce Immutability**
- **NEVER** modify existing rubric configurations
- Updates create new version (e.g., 1.0.0 → 1.1.0)
- Set `EffectiveTo` on old version when new version activates
- Preserve old version for historical assessment reproducibility

---

### 5. **Provide Scoring Transparency**
- Store full rubric JSON snapshot in `ConfigurationJson` field
- Assessment API should return which rubric was used
- Admin can re-calculate assessment with different rubric (comparison mode)
- Audit log for rubric changes (who, when, what changed)

---

### 6. **Start Simple, Add Complexity Later**

**Phase 1-2 (Simple):**
- Basic operators: `>=`, `<`, `Between`
- Single metric per threshold
- Fixed status ranges (Served/Underserved/Unserved)

**Phase 4+ (Complex):**
- Composite conditions: `(Speed >= 25 AND Latency < 50) OR HasFiber`
- Custom status levels: "Marginally Served", "Critically Unserved"
- Weighted scoring: Different weights per criterion
- Regional overrides: Urban vs rural thresholds

---

### 7. **Performance Monitoring**
- Benchmark scoring performance before and after refactoring
- Target: <10% overhead compared to hardcoded logic
- Monitor: Rubric cache hit rate (target >90%)
- Alert: If assessment job duration increases >20%

---

## Conclusion

### Summary

Transforming the CMTS rubric from hardcoded logic to a dynamic, database-driven system requires:

1. **Database Layer:** 4 new tables for rubric configuration, criteria, thresholds, status ranges
2. **Service Layer:** New `IRubricScoringService` with dynamic threshold evaluation
3. **API Layer:** Rubric management endpoints + enhanced assessment endpoints
4. **Data Migration:** Backfill historical assessments with default rubric reference
5. **Testing:** Comprehensive unit/integration tests to ensure scoring parity

### Benefits

✅ **Flexibility:** Adjust scoring without code deployment  
✅ **Versioning:** Historical assessments remain valid and reproducible  
✅ **Scalability:** Add new criteria/thresholds via configuration  
✅ **Transparency:** Users see which rubric was used for each assessment  
✅ **Compliance:** Audit trail for regulatory requirements  
✅ **Innovation:** A/B test rubric variations, regional customization  

### Next Steps

1. **Review & Approve:** Stakeholder review of this document
2. **Database Design:** Finalize schema and create migration script
3. **Seed Data:** Create default CMTS v1.0.0 rubric matching current logic
4. **POC:** Implement scoring service for 1 criterion (Internet Access) to validate approach
5. **Parity Testing:** Ensure dynamic scoring matches hardcoded logic 100%
6. **Phased Rollout:** Follow 10-week implementation plan

---

**Document Author:** GitHub Copilot  
**Review Status:** Draft - Pending Stakeholder Approval  
**Est. Implementation Time:** 10 weeks (2 engineers)  
**Est. Complexity:** High (architectural change across 5 layers)
