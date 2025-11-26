# Connectivity Assessment Scheduler - System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            API LAYER (Controllers)                           │
│                                                                              │
│  SchedulersController                                                        │
│  ├─ GET    /api/schedulers          → GetAllSchedulersRequest              │
│  ├─ GET    /api/schedulers/{id}     → GetSchedulerByIdRequest              │
│  ├─ POST   /api/schedulers          → CreateSchedulerRequest               │
│  ├─ PUT    /api/schedulers/{id}     → UpdateSchedulerRequest               │
│  ├─ DELETE /api/schedulers/{id}     → DeleteSchedulerRequest               │
│  ├─ POST   /api/schedulers/{id}/pause  → PauseSchedulerRequest            │
│  └─ POST   /api/schedulers/{id}/resume → ResumeSchedulerRequest           │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   │ MediatR
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER (Handlers)                          │
│                                                                              │
│  Command Handlers                    Query Handlers                         │
│  ├─ CreateSchedulerHandler          ├─ GetAllSchedulersHandler             │
│  ├─ UpdateSchedulerHandler          └─ GetSchedulerByIdHandler             │
│  ├─ DeleteSchedulerHandler                                                  │
│  ├─ PauseSchedulerHandler                                                   │
│  └─ ResumeSchedulerHandler                                                  │
└──────────────┬──────────────────────────────────┬───────────────────────────┘
               │                                  │
               │                                  │
               ▼                                  ▼
┌───────────────────────────────┐   ┌────────────────────────────────────────┐
│   DOMAIN SERVICES             │   │   REPOSITORY LAYER                     │
│                               │   │                                        │
│  ISchedulerService            │   │  IConnectivityAssessmentScheduler-     │
│  └─ SchedulerService          │   │  Repository                            │
│     ├─ RegisterScheduler()    │   │  └─ ConnectivityAssessmentScheduler-  │
│     ├─ UnregisterScheduler()  │   │     Repository                         │
│     ├─ PauseScheduler()       │   │     ├─ GetActiveSchedulers()          │
│     ├─ ResumeScheduler()      │   │     ├─ GetByHangfireJobId()          │
│     ├─ GenerateCronExpression()│  │     ├─ UpdateExecutionStats()         │
│     ├─ ParseConfiguration()   │   │     └─ GetSchedulersToProcess()       │
│     └─ GetNextExecutionTime() │   │                                        │
└───────┬───────────────────────┘   └────────────────┬───────────────────────┘
        │                                            │
        │                                            │
        ▼                                            ▼
┌────────────────────────┐              ┌──────────────────────────────────────┐
│   HANGFIRE SERVICE     │              │   DATABASE                            │
│                        │              │                                       │
│  IBackgroundJobService │              │  ConnectivityAssessmentSchedulers     │
│  ├─ AddOrUpdateRecurring│             │  ├─ Id (Guid)                        │
│  │  Job()              │              │  ├─ HangfireJobId (string)           │
│  └─ RemoveRecurring-   │              │  ├─ Name (string)                    │
│     Job()              │              │  ├─ Frequency (enum)                 │
└────────┬───────────────┘              │  ├─ CronExpression (string)          │
         │                               │  ├─ LocationType (enum)              │
         │                               │  ├─ LocationIdsJson (json)           │
         │                               │  ├─ Status (enum)                    │
         │                               │  ├─ NextExecutionTime (datetime?)    │
         │                               │  ├─ ExecutionCount (int)             │
         │                               │  └─ FailureCount (int)               │
         │                               └──────────────────────────────────────┘
         │
         │ Creates/Manages
         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            HANGFIRE DASHBOARD                                │
│                                                                              │
│  Recurring Jobs                                                              │
│  ├─ ConnectivityAssessment_xxxx  [Every day at 2:00 AM]                    │
│  ├─ ConnectivityAssessment_yyyy  [Every Mon,Wed,Fri at 9:00 AM]            │
│  └─ ConnectivityAssessment_zzzz  [Every 1st of month at 12:00 AM]          │
│                                                                              │
│  When cron schedule triggers ──────────────────────────┐                    │
└────────────────────────────────────────────────────────┼────────────────────┘
                                                         │
                                                         │ Executes
                                                         ▼
                                         ┌───────────────────────────────────┐
                                         │ ProcessConnectivityAssessmentJob  │
                                         │                                   │
                                         │  ExecuteAsync()                   │
                                         │  ├─ Load locations                │
                                         │  ├─ Process assessments           │
                                         │  ├─ Update progress               │
                                         │  └─ Complete job                  │
                                         └───────────────────────────────────┘


WORKFLOW EXAMPLE: Creating a Daily Schedule
═══════════════════════════════════════════

1. Admin sends POST to /api/schedulers
   ↓
2. CreateSchedulerHandler receives request
   ↓
3. SchedulerService generates cron expression
   "0 2 * * *" (Daily at 2:00 AM)
   ↓
4. Repository saves to database
   ↓
5. SchedulerService registers with Hangfire
   ↓
6. Hangfire creates recurring job
   ↓
7. At 2:00 AM daily, Hangfire triggers job
   ↓
8. ProcessConnectivityAssessmentJob executes
   ↓
9. Repository updates execution stats
   (ExecutionCount++, LastExecutionTime, etc.)


CRON EXPRESSION GENERATION
═══════════════════════════

Frequency: Hourly
Config: { minuteOfHour: 30 }
Cron: "30 * * * *"
Meaning: Every hour at :30

Frequency: Daily
Config: { hourOfDay: 2, minuteOfDay: 0 }
Cron: "0 2 * * *"
Meaning: Every day at 2:00 AM

Frequency: Weekly
Config: { daysOfWeek: [1,3,5], weeklyHour: 9, weeklyMinute: 0 }
Cron: "0 9 * * 1,3,5"
Meaning: Mon, Wed, Fri at 9:00 AM

Frequency: Monthly
Config: { dayOfMonth: 1, monthlyHour: 0, monthlyMinute: 0 }
Cron: "0 0 1 * *"
Meaning: 1st of every month at midnight

Frequency: Custom
Config: { customCronExpression: "0 */6 * * *" }
Cron: "0 */6 * * *"
Meaning: Every 6 hours


DATA FLOW
═════════

┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Admin   │───▶│   API    │───▶│ Handler  │───▶│ Service  │───▶│ Hangfire │
│  Input   │    │Controller│    │ (MediatR)│    │          │    │          │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
                                      │                                │
                                      ▼                                │
                                ┌──────────┐                          │
                                │Repository│                          │
                                │          │                          │
                                └────┬─────┘                          │
                                     ▼                                 │
                                ┌──────────┐                          │
                                │ Database │                          │
                                └──────────┘                          │
                                                                       │
                                                                       ▼
                                                         ┌──────────────────────┐
                                                         │ Connectivity         │
                                                         │ Assessment Job       │
                                                         │ (Auto-triggered)     │
                                                         └──────────────────────┘
```
