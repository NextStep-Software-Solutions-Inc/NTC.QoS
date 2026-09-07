# Connectivity Assessment Scheduler System

This document describes the new scheduler system for automating connectivity assessments.

## Overview

The scheduler system allows administrators to create automated, recurring connectivity assessments with flexible scheduling options (hourly, daily, weekly, monthly, or custom cron expressions).

## Components Created

### 1. Domain Layer

#### Entities
- **`ConnectivityAssessmentScheduler`** (`NTC.QoS.Domain/Entities/ConnectivityAssessmentScheduler.cs`)
  - Stores scheduler configuration and execution history
  - Tracks status, execution counts, failures, and next execution times

#### Enums
- **`ScheduleFrequencyEnum`** (`NTC.QoS.Domain/Enums/ScheduleFrequencyEnum.cs`)
  - Once, Hourly, Daily, Weekly, Monthly, Custom
  
- **`SchedulerStatusEnum`** (`NTC.QoS.Domain/Enums/SchedulerStatusEnum.cs`)
  - Active, Paused, Completed, Failed

#### DTOs
- **`CreateSchedulerDto`** - For creating new schedules
- **`UpdateSchedulerDto`** - For updating existing schedules
- **`SchedulerResponseDto`** - For returning scheduler data
- **`ScheduleConfigurationDto`** - Detailed configuration per frequency type

#### Interfaces
- **`IConnectivityAssessmentSchedulerRepository`** - Repository interface
- **`ISchedulerService`** - Service for managing Hangfire jobs and cron expressions

### 2. Infrastructure Layer

#### Repositories
- **`ConnectivityAssessmentSchedulerRepository`** (`NTC.QoS.Infrastructure/Persistence/Repositories/`)
  - Implements CRUD operations
  - Methods for getting active schedulers, updating execution stats

#### Services
- **`SchedulerService`** (`NTC.QoS.Infrastructure/Services/SchedulerService.cs`)
  - Registers/unregisters Hangfire recurring jobs
  - Generates cron expressions based on configuration
  - Calculates next execution times

### 3. Application Layer

#### MediatR Requests (Commands)
- **`CreateSchedulerRequest`** - Create new schedule
- **`UpdateSchedulerRequest`** - Update existing schedule
- **`DeleteSchedulerRequest`** - Delete schedule
- **`PauseSchedulerRequest`** - Pause active schedule
- **`ResumeSchedulerRequest`** - Resume paused schedule

#### MediatR Requests (Queries)
- **`GetAllSchedulersRequest`** - Get paginated list of schedules
- **`GetSchedulerByIdRequest`** - Get specific schedule

#### Handlers
All corresponding handlers implemented in:
- `NTC.QoS.Application/Features/Scheduler/Handlers/Commands/`
- `NTC.QoS.Application/Features/Scheduler/Handlers/Queries/`

### 4. API Layer

#### Controller
- **`SchedulersController`** (`NTC.QoS.API/Controllers/SchedulersController.cs`)
  - RESTful endpoints for scheduler management
  - Routes: `api/schedulers`

## API Endpoints

### GET /api/schedulers
Get all schedules with pagination and optional search
- Query params: `pageNumber`, `pageSize`, `searchTerm`

### GET /api/schedulers/{id}
Get specific schedule by ID

### POST /api/schedulers
Create new schedule
```json
{
  "name": "Daily Assessment - Region 1",
  "description": "Run connectivity assessment daily at 2 AM",
  "frequency": 2,  // Daily
  "locationType": 3,  // Region
  "locationIds": ["<region-guid>"],
  "timeZone": "Asia/Manila",
  "configuration": {
    "hourOfDay": 2,
    "minuteOfDay": 0
  }
}
```

### PUT /api/schedulers/{id}
Update existing schedule

### DELETE /api/schedulers/{id}
Delete schedule (soft delete + unregister from Hangfire)

### POST /api/schedulers/{id}/pause
Pause active schedule

### POST /api/schedulers/{id}/resume
Resume paused schedule

## Schedule Configuration Examples

### Hourly Schedule
```json
{
  "frequency": 1,
  "configuration": {
    "minuteOfHour": 30  // Run at :30 of every hour
  }
}
```

### Daily Schedule
```json
{
  "frequency": 2,
  "configuration": {
    "hourOfDay": 3,      // 3 AM
    "minuteOfDay": 15    // 3:15 AM
  }
}
```

### Weekly Schedule
```json
{
  "frequency": 3,
  "configuration": {
    "daysOfWeek": [1, 3, 5],  // Monday, Wednesday, Friday
    "weeklyHour": 9,           // 9 AM
    "weeklyMinute": 0          // 9:00 AM
  }
}
```

### Monthly Schedule
```json
{
  "frequency": 4,
  "configuration": {
    "dayOfMonth": 1,      // 1st of month
    "monthlyHour": 0,     // Midnight
    "monthlyMinute": 0    // 12:00 AM
  }
}
```

### Custom Cron Schedule
```json
{
  "frequency": 5,
  "configuration": {
    "customCronExpression": "0 */6 * * *"  // Every 6 hours
  }
}
```

## Database Migration

You need to create and apply a migration for the new `ConnectivityAssessmentSchedulers` table:

```bash
# Create migration
dotnet ef migrations add AddConnectivityAssessmentScheduler --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API

# Apply migration
dotnet ef database update --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API
```

## Dependencies

The scheduler system uses:
- **Hangfire** - For recurring job management (already in your project)
- **Cronos** - For parsing and calculating cron expressions (needs to be added)

Add the Cronos package:
```bash
dotnet add NTC.QoS.Infrastructure package Cronos
```

## How It Works

1. **Admin creates schedule** via POST `/api/schedulers`
2. **SchedulerService generates cron expression** based on frequency and configuration
3. **Hangfire recurring job is registered** with the generated cron expression
4. **Job executes automatically** on schedule, calling `ProcessConnectivityAssessmentJob.ExecuteAsync()`
5. **Repository tracks execution stats** - count, failures, last/next execution times
6. **Admin can pause/resume/delete** schedules as needed

## Features

- ✅ Flexible scheduling (hourly, daily, weekly, monthly, custom cron)
- ✅ Timezone support
- ✅ Start/end date constraints
- ✅ Pause/resume capability
- ✅ Execution tracking (count, failures, last run)
- ✅ Automatic failure handling (pauses after 5 failures)
- ✅ Search and pagination
- ✅ Email notifications support (configured but implementation TBD)
- ✅ Clean architecture with proper separation of concerns

## Security

All endpoints require authentication via `[Authorize]` attribute. Consider adding role-based authorization for admin-only access:

```csharp
[Authorize(Roles = "Admin")]
public class SchedulersController : ControllerBase
```

## Future Enhancements

1. Implement email notification system when jobs complete
2. Add job execution history table for detailed audit trail
3. Add webhook support for job completion events
4. Dashboard UI for visualizing scheduler status
5. Bulk schedule creation for multiple locations
