# Connectivity Assessment Scheduler - Quick Start Guide

## Setup Instructions

### 1. Create Database Migration

```bash
# Navigate to project root
cd /c/Users/almau/NTC.QoS-Backend

# Create migration
dotnet ef migrations add AddConnectivityAssessmentScheduler \
  --project NTC.QoS.Infrastructure \
  --startup-project NTC.QoS.API

# Apply migration to database
dotnet ef database update \
  --project NTC.QoS.Infrastructure \
  --startup-project NTC.QoS.API
```

### 2. Verify Installation

All necessary packages and services are already registered:
- ✅ Cronos package added to `NTC.QoS.Infrastructure`
- ✅ Repository registered in DI
- ✅ Scheduler service registered in DI
- ✅ Controller endpoints created
- ✅ MediatR handlers configured

### 3. Test the API

After running the migration, start the API and test the scheduler endpoints:

```bash
# Build and run
dotnet run --project NTC.QoS.API
```

## Usage Examples

### Example 1: Daily Assessment at 2 AM (Manila Time)

```bash
curl -X POST https://localhost:5001/api/schedulers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Daily Region 1 Assessment",
    "description": "Automated daily connectivity assessment for Region 1",
    "frequency": 2,
    "locationType": 3,
    "locationIds": ["your-region-guid-here"],
    "timeZone": "Asia/Manila",
    "configuration": {
      "hourOfDay": 2,
      "minuteOfDay": 0
    }
  }'
```

### Example 2: Weekly Assessment (Mon, Wed, Fri at 9 AM)

```bash
curl -X POST https://localhost:5001/api/schedulers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Weekly Province Assessment",
    "description": "Three times a week assessment",
    "frequency": 3,
    "locationType": 2,
    "locationIds": ["province-guid-1", "province-guid-2"],
    "timeZone": "Asia/Manila",
    "configuration": {
      "daysOfWeek": [1, 3, 5],
      "weeklyHour": 9,
      "weeklyMinute": 0
    }
  }'
```

### Example 3: Hourly Assessment

```bash
curl -X POST https://localhost:5001/api/schedulers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Hourly Municipality Check",
    "description": "High-frequency monitoring",
    "frequency": 1,
    "locationType": 1,
    "locationIds": ["municipality-guid"],
    "timeZone": "Asia/Manila",
    "configuration": {
      "minuteOfHour": 30
    }
  }'
```

### Example 4: Monthly Assessment (1st of Month at Midnight)

```bash
curl -X POST https://localhost:5001/api/schedulers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Monthly National Report",
    "description": "Comprehensive monthly assessment",
    "frequency": 4,
    "locationType": 3,
    "locationIds": ["all-regions"],
    "timeZone": "Asia/Manila",
    "configuration": {
      "dayOfMonth": 1,
      "monthlyHour": 0,
      "monthlyMinute": 0
    },
    "sendNotifications": true,
    "notificationEmails": ["admin@example.com"]
  }'
```

## Managing Schedules

### Get All Schedules
```bash
curl -X GET https://localhost:5001/api/schedulers?pageNumber=1&pageSize=20 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Get Specific Schedule
```bash
curl -X GET https://localhost:5001/api/schedulers/{scheduler-id} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Pause Schedule
```bash
curl -X POST https://localhost:5001/api/schedulers/{scheduler-id}/pause \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Resume Schedule
```bash
curl -X POST https://localhost:5001/api/schedulers/{scheduler-id}/resume \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Update Schedule
```bash
curl -X PUT https://localhost:5001/api/schedulers/{scheduler-id} \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Updated Schedule Name",
    "configuration": {
      "hourOfDay": 3
    }
  }'
```

### Delete Schedule
```bash
curl -X DELETE https://localhost:5001/api/schedulers/{scheduler-id} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Frequency Options Reference

| Frequency | Value | Required Configuration Fields |
|-----------|-------|------------------------------|
| Once      | 0     | N/A (use start date)        |
| Hourly    | 1     | `minuteOfHour`              |
| Daily     | 2     | `hourOfDay`, `minuteOfDay`  |
| Weekly    | 3     | `daysOfWeek`, `weeklyHour`, `weeklyMinute` |
| Monthly   | 4     | `dayOfMonth`, `monthlyHour`, `monthlyMinute` |
| Custom    | 5     | `customCronExpression`      |

## Location Type Reference

| Location Type | Value |
|--------------|-------|
| Barangay     | 0     |
| Municipality | 1     |
| Province     | 2     |
| Region       | 3     |

## Monitoring

### Check Hangfire Dashboard
Visit `https://localhost:5001/hangfire` to view:
- Active recurring jobs
- Job execution history
- Failed jobs
- Performance metrics

### View Scheduler Status
The scheduler entity tracks:
- `lastExecutionTime` - When the job last ran
- `nextExecutionTime` - When it will run next
- `executionCount` - Total successful runs
- `failureCount` - Total failures
- `status` - Active, Paused, Completed, Failed
- `lastError` - Most recent error message

## Troubleshooting

### Scheduler Not Running
1. Check scheduler status is `Active` (not `Paused` or `Failed`)
2. Verify Hangfire is running (check dashboard)
3. Check `nextExecutionTime` is in the future
4. Verify timezone is correct
5. Check `startDate` and `endDate` constraints

### Job Failing
1. Check `lastError` field for error message
2. View Hangfire dashboard for detailed stack trace
3. Verify location IDs are valid
4. Check database connectivity
5. Review API logs

### Auto-Pause After Failures
Schedulers automatically pause after 5 consecutive failures to prevent spam. Check:
1. `failureCount` field
2. `status` field (will be `Failed`)
3. Fix the underlying issue
4. Resume the scheduler via `/resume` endpoint

## Best Practices

1. **Use appropriate frequencies** - Don't run assessments more frequently than needed
2. **Set timezone correctly** - Use IANA timezone identifiers (e.g., "Asia/Manila")
3. **Monitor execution stats** - Check failure counts regularly
4. **Use start/end dates** - For time-bounded schedules
5. **Test with small datasets first** - Before scheduling large regions
6. **Use search functionality** - Name schedules descriptively for easy filtering
7. **Consider off-peak hours** - Schedule intensive jobs during low-traffic times

## Next Steps

1. Add role-based authorization (Admin only)
2. Implement email notifications
3. Create admin UI dashboard
4. Add execution history table
5. Implement webhook notifications
