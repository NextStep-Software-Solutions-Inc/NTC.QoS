# 📚 NTC QoS Documentation Hub

Welcome to the central documentation index for the **NTC Quality of Service (QoS) Management System**. This documentation hub provides structured technical references, architecture diagrams, process flows, API contracts, and implementation guides categorized for **Frontend (FE)** and **Backend (BE)** engineering.

---

## 🧭 Directory Structure

```
docs/
├── README.md                                       # This index file
├── frontend/                                       # Frontend (Next.js 16 + React 19 + Bun) documentation
└── backend/                                        # Backend (.NET 8 + PostgreSQL + PostGIS + pgvector) documentation
    └── ConnectivityAssessmentScheduler/            # Assessment scheduler subsystem
```

---

## 🎨 Frontend (FE) Documentation (`docs/frontend/`)

Documentation covering the Next.js 16 web application, GIS/Map rendering, Web Workers, state management, and UI architecture:

| Document | Topic / Area | Summary |
|---|---|---|
| [ABORT_CONTROLLER_USAGE.md](./frontend/ABORT_CONTROLLER_USAGE.md) | Network / React Query | Guide on using `AbortController` cancellation in mutation hooks |
| [AI_FEATURES_DOCUMENTATION.md](./frontend/AI_FEATURES_DOCUMENTATION.md) | GenAI / UI/UX | Visual and architectural guide for AI Chat Assistant & Report Generation |
| [BARANGAY_COVERAGE_API_UPDATE.md](./frontend/BARANGAY_COVERAGE_API_UPDATE.md) | API Integration | Optimistic updates & deletion flow for Barangay Coverage APIs |
| [BARANGAY_COVERAGE_PERFORMANCE_OPTIMIZATIONS.md](./frontend/BARANGAY_COVERAGE_PERFORMANCE_OPTIMIZATIONS.md) | Performance | Memoization, prop equality, and containment for massive barangay lists |
| [BARANGAY_COVERAGE_UI_DESIGN.md](./frontend/BARANGAY_COVERAGE_UI_DESIGN.md) | UI / Styling | Pastel theme, layout structure, and hierarchical accordion design |
| [BARANGAY_SEARCH_IMPLEMENTATION.md](./frontend/BARANGAY_SEARCH_IMPLEMENTATION.md) | Search / GIS | Integration of MapTiler geocoding with backend Barangay boundary search |
| [BULK_DELETE_PROGRESS_DIALOG.md](./frontend/BULK_DELETE_PROGRESS_DIALOG.md) | UI Components | Progress tracking modal with batch counters for bulk deletion |
| [CACHE_REFRESH_OPTIMIZATION.md](./frontend/CACHE_REFRESH_OPTIMIZATION.md) | Performance | Eliminating UI delay during cache invalidation in React Query |
| [CELLSITE_MANAGEMENT.md](./frontend/CELLSITE_MANAGEMENT.md) | Feature Architecture | Cell site tower management, table filtering, and multi-region monitoring |
| [CELLSITE_MAP_API_INTEGRATION.md](./frontend/CELLSITE_MAP_API_INTEGRATION.md) | API Integration | Query parameters and schema contract for map-based cell filtering |
| [CELL_SITE_INLINE_EDIT_ARCHITECTURE.md](./frontend/CELL_SITE_INLINE_EDIT_ARCHITECTURE.md) | Architecture / State | Detail page inline editing state machine, rollback, and field validation |
| [CELL_SITE_UI_DESIGN_REFERENCE.md](./frontend/CELL_SITE_UI_DESIGN_REFERENCE.md) | UI / Layout | Visual layouts for cell management, filters, and stat badges |
| [CELL_WORKER_IMPLEMENTATION.md](./frontend/CELL_WORKER_IMPLEMENTATION.md) | Web Workers / GIS | Off-thread GeoJSON polygon computation using Turf.js in Web Workers |
| [CLICKABLE_STATUS_BADGE_ARCHITECTURE.md](./frontend/CLICKABLE_STATUS_BADGE_ARCHITECTURE.md) | UI Architecture | In-place status badge toggle, optimistic mutation, and event propagation |
| [MANAGE_BARANGAY_COVERAGE_REFACTORING.md](./frontend/MANAGE_BARANGAY_COVERAGE_REFACTORING.md) | Refactoring / SOLID | Modular component decomposition of the barangay coverage module |
| [NAP_ANALYTICS_API_INTEGRATION.md](./frontend/NAP_ANALYTICS_API_INTEGRATION.md) | Analytics / API | Server-side NAP analytics integration into the analytics panel |
| [NAP_BULK_DELETE_OPTIMIZATION.md](./frontend/NAP_BULK_DELETE_OPTIMIZATION.md) | Performance / API | Chunked URL-safe batch deletion strategy (100 IDs per batch) |
| [PAGINATION_IMPLEMENTATION_GUIDE.md](./frontend/PAGINATION_IMPLEMENTATION_GUIDE.md) | Navigation / URL State | URL search parameter synchronized pagination pattern |
| [REPORTS_IMPLEMENTATION.md](./frontend/REPORTS_IMPLEMENTATION.md) | Reports / Exports | Broadband coverage report builder, filters, and PDF export implementation |
| [REPORTS_UI_DESIGN.md](./frontend/REPORTS_UI_DESIGN.md) | UI / Layout | Visual mockups and navigation flow for report generation screens |
| [SPEEDTEST_ANALYTICS_API_INTEGRATION.md](./frontend/SPEEDTEST_ANALYTICS_API_INTEGRATION.md) | Analytics / API | Speedtest dashboard backend analytics query handler integration |
| [SUPER_ADMIN_IMPLEMENTATION.md](./frontend/SUPER_ADMIN_IMPLEMENTATION.md) | Admin / CRUD | Super Admin console layout, permission matrices, and CRUD views |
| [TECHNICAL_DEBT.md](./frontend/TECHNICAL_DEBT.md) | Maintenance | Frontend technical debt log and recommended refactorings |
| [VIEWPORT_BASED_LOADING.md](./frontend/VIEWPORT_BASED_LOADING.md) | GIS / Performance | Viewport bounding-box data loading for scalable map visualization |
| [WEB_WORKER_TESTING_GUIDE.md](./frontend/WEB_WORKER_TESTING_GUIDE.md) | Testing / Verification | DevTools verification guide and message tracking for Web Workers |

---

## ⚙️ Backend (BE) Documentation (`docs/backend/`)

Documentation covering the .NET 8 Web API, Domain/Application architecture, PostGIS spatial queries, PostgreSQL optimizations, and background jobs:

### Architecture, Background Jobs & Process Flows
| Document | Summary |
|---|---|
| [ProcessConnectivityAssessmentJob_ProcessFlow.md](./backend/ProcessConnectivityAssessmentJob_ProcessFlow.md) | Hangfire background job pipeline for computing connectivity assessments |
| [WirelessCoverageComputationService_ProcessFlow.md](./backend/WirelessCoverageComputationService_ProcessFlow.md) | Detailed process flow for wireless spatial intersection and scoring |
| [WIRELESS_COVERAGE_ASSESSMENT_IMPLEMENTATION.md](./backend/WIRELESS_COVERAGE_ASSESSMENT_IMPLEMENTATION.md) | Assessment generation trigger, job tracking, and status polling |
| [Cell_Processing_Enhancement_Summary.md](./backend/Cell_Processing_Enhancement_Summary.md) | Cell file upload processing pipeline and bulk database persistence |
| [Cell_Upload_Warnings_Quick_Reference.md](./backend/Cell_Upload_Warnings_Quick_Reference.md) | Detailed reference of validation warnings generated during cell imports |
| [MANUAL_CELLULAR_HANDLER_LOGIC.md](./backend/MANUAL_CELLULAR_HANDLER_LOGIC.md) | Frequency calculations (DL/UL/Bandwidth), network creation, and manual cell handling |
| [NAP-Processing-Optimization.md](./backend/NAP-Processing-Optimization.md) | Spatial query optimizations and memory management for NAP processing |
| [INFRASTRUCTURE_IMPROVEMENTS_SUMMARY.md](./backend/INFRASTRUCTURE_IMPROVEMENTS_SUMMARY.md) | Clean Architecture dependency injection and repository refactoring |

### Scoring Rubrics & Business Logic
| Document | Summary |
|---|---|
| [WIRELESS_BROADBAND_REPORT_LOGIC.md](./backend/WIRELESS_BROADBAND_REPORT_LOGIC.md) | CMTS Broadband Mapping Rubric scoring logic for Wired vs Wireless coverage |
| [Wireless_Broadband_Scored_Report_Refactoring.md](./backend/Wireless_Broadband_Scored_Report_Refactoring.md) | Architecture refactoring for scored report generation |
| [SignalStrengthUtilities.md](./backend/SignalStrengthUtilities.md) | Signal level classification helper extension methods and thresholds |

### AI, Semantic Search & MCP
| Document | Summary |
|---|---|
| [MCP_IMPLEMENTATION_GUIDE.md](./backend/MCP_IMPLEMENTATION_GUIDE.md) | Model Context Protocol (MCP) server architecture and database query tools |
| [GENAI_INTEGRATION.md](./backend/GENAI_INTEGRATION.md) | Google GenAI SDK integration, streaming handlers, and chat architecture |
| [MIGRATION_REVIEW_RECOMMENDATIONS.md](./backend/MIGRATION_REVIEW_RECOMMENDATIONS.md) | PostgreSQL `pgvector` extension setup, embeddings, and vector migration guide |
| [OPTION1_IMPLEMENTATION_VERIFICATION.md](./backend/OPTION1_IMPLEMENTATION_VERIFICATION.md) | Verification and schema check for vector embeddings and report templates |

### Database, Seeders & Postmortems
| Document | Summary |
|---|---|
| [LocationSeederRefactoring.md](./backend/LocationSeederRefactoring.md) | GeoJSON boundary batching and spatial indexing performance optimizations |
| [PopulationSeeder.md](./backend/PopulationSeeder.md) | Location and population data seeding architecture (Cloud Blob Storage & GeoJSON) |
| [DEFAULT_ADMIN_SETUP.md](./backend/DEFAULT_ADMIN_SETUP.md) | Initial system admin user setup and credential configuration |
| [PRODUCTION_DATABASE_ISSUE_REPORT.md](./backend/PRODUCTION_DATABASE_ISSUE_REPORT.md) | Advisory lock deadlock investigation postmortem and recovery script |

### API Contracts & Testing
| Document | Summary |
|---|---|
| [ConnectivityAnalysisAPI.md](./backend/ConnectivityAnalysisAPI.md) | Complete OpenAPI specification for connectivity assessment queries |
| [WIRELESS_BROADBAND_EXECUTIVE_SUMMARY_API.md](./backend/WIRELESS_BROADBAND_EXECUTIVE_SUMMARY_API.md) | Executive summary report endpoints and aggregation parameters |
| [SignalLevelEntries_API.md](./backend/SignalLevelEntries_API.md) | Signal level entries data ingestion API contract |
| [BarangayAPI_UI_Implementation_Guide.md](./backend/BarangayAPI_UI_Implementation_Guide.md) | Barangay administrative boundary queries and UI contract |
| [RATE_LIMIT_TESTING.md](./backend/RATE_LIMIT_TESTING.md) | API rate limiting test script (`test-rate-limit.sh`) usage guide |
| [DEPLOYMENT_SECURITY_CHECKLIST.md](./backend/DEPLOYMENT_SECURITY_CHECKLIST.md) | Pre-deployment security verification, HTTPS, CORS, and auth checklist |

---

### ⏱️ Connectivity Assessment Scheduler Subsystem (`docs/backend/ConnectivityAssessmentScheduler/`)

| Document | Summary |
|---|---|
| [CONNECTIVITY_ASSESSMENT_SCHEDULER.md](./backend/ConnectivityAssessmentScheduler/CONNECTIVITY_ASSESSMENT_SCHEDULER.md) | Subsystem overview, cron triggers, and execution lifecycle |
| [SCHEDULER_ARCHITECTURE_DIAGRAM.md](./backend/ConnectivityAssessmentScheduler/SCHEDULER_ARCHITECTURE_DIAGRAM.md) | Visual sequence diagram of recurring assessment jobs |
| [SCHEDULER_QUICK_START.md](./backend/ConnectivityAssessmentScheduler/SCHEDULER_QUICK_START.md) | Getting started guide for setting up and running scheduled assessments |
| [SCHEDULER_TROUBLESHOOTING.md](./backend/ConnectivityAssessmentScheduler/SCHEDULER_TROUBLESHOOTING.md) | Common scheduler errors, timeout handling, and job retries |
| [SCHEDULER_END_DATE_FIXES.md](./backend/ConnectivityAssessmentScheduler/SCHEDULER_END_DATE_FIXES.md) | Date boundary calculation fixes for quarterly and monthly windows |
| [SCHEDULER_FIXES_APPLIED.md](./backend/ConnectivityAssessmentScheduler/SCHEDULER_FIXES_APPLIED.md) | Summary of fixes applied to scheduler job parameters |
| [SCHEDULER_IMPLEMENTATION_SUMMARY.md](./backend/ConnectivityAssessmentScheduler/SCHEDULER_IMPLEMENTATION_SUMMARY.md) | Implementation summary of the scheduler subsystem |

---

## 🔍 System Prompt & Runtime Template Locations

For reference, the following files are active runtime components located within their respective module directories:

- **AI Base Prompt**: [`NTC.QoS.API/Global AI Instruction.md`](../NTC.QoS.API/Global%20AI%20Instruction.md) (Loaded at startup by `AIInstructionService.cs`)
- **AI RAG Module**: [`NTC.QoS.Application/Features/GenAI/README.md`](../NTC.QoS.Application/Features/GenAI/README.md)
- **AI Report Templates**: [`NTC.QoS.Web/public/report-templates/*.md`](../NTC.QoS.Web/public/report-templates/) (Fetched at runtime by frontend AI report generators)
- **Component Docs**:
  - [`NTC.QoS.Web/src/components/admin-map/README.md`](../NTC.QoS.Web/src/components/admin-map/README.md)
  - [`NTC.QoS.Web/src/components/ManageBarangayCoverage/README.md`](../NTC.QoS.Web/src/components/ManageBarangayCoverage/README.md)
