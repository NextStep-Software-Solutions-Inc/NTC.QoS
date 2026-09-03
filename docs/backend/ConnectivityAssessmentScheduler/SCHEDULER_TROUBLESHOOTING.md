# Connectivity Assessment Scheduler - Troubleshooting Guide

## Issue: Schedule Was Created But Didn't Run (nextExecutionTime is null)

### Symptoms
- Schedule created successfully (Status: `Active`)
- `executionCount` = 0
- `lastExecutionTime` = null
- **`nextExecutionTime` = null** ← Critical indicator
- Schedule time has already passed

### Root Causes & Solutions

---

## 1. nextExecutionTime is Null (Most Common)

### Problem
The scheduler entity was saved to the database, but Hangfire hasn't picked up the job yet or failed to register it.

### Why It Happens
- **Race condition:** Job created too close to scheduled execution time
- **Hangfire polling delay:** Background server checks for new jobs every 15 seconds to 5 minutes
- **Registration failure:** Timezone error, cron parse error, or Hangfire server not running

### Solution Applied
The updated code now:
1. **Validates timezone BEFORE creating the schedule**
2. **Registers with Hangfire IMMEDIATELY after DB save**
3. **Updates nextExecutionTime after registration**
4. **Adds comprehensive logging**

### Verification
```sql
SELECT 
    "Id",
    "Name",
    "Status",
    "NextExecutionTime",
    "CronExpression",
    "TimeZone",
    "CreatedAt",
    "UpdatedAt"
FROM "ConnectivityAssessmentSchedulers"
WHERE "IsDeleted" = FALSE
ORDER BY "CreatedAt" DESC;
```

If `NextExecutionTime` is still null after creation:
- Check application logs for errors
- Restart Hangfire server
- Verify Hangfire dashboard shows the recurring job

---

## 2. Tight Update Window (2-5 Minutes Before Run)

### Problem
```
14:01:44 - Job created
14:07:10 - Job updated (configuration changed)
14:10:00 - Scheduled to run
14:10:00 - MISSED - Hangfire didn't pick up the change
```

If you update a schedule within 5 minutes of the next execution, Hangfire may not pick up the change in time.

### Why It Happens
- Hangfire polls the database periodically (default: every 15 seconds)
- If the update happens at 14:07 and the job is scheduled for 14:10, there's only a 3-minute window
- If Hangfire last checked at 14:06:50 and won't check again until 14:08:50, it might calculate the next run as tomorrow

### Solutions

#### Option 1: Pause → Update → Resume
```bash
# Pause the schedule
POST /api/schedulers/{id}/pause

# Make your updates
PUT /api/schedulers/{id}

# Resume (forces re-registration)
POST /api/schedulers/{id}/resume
```

#### Option 2: Delete → Recreate
For critical timing, delete and recreate the schedule:
```bash
DELETE /api/schedulers/{id}
POST /api/schedulers  # Create new with desired schedule
```

#### Option 3: Manual Trigger (If Available)
Trigger a one-time execution while waiting for the schedule:
```bash
POST /api/analytics/connectivity-assessment/start
{
  "locationIds": [...],
  "locationType": 3
}
```

---

## 3. Timezone Mismatch

### Problem
Schedule shows `"timeZone": "Asia/Manila"` but server doesn't recognize it.

### Symptoms
- Job runs at wrong time (e.g., 10 PM instead of 2 PM)
- OR `nextExecutionTime` = null
- Application logs show `TimeZoneNotFoundException`

### Platform-Specific Issues

#### Windows Server
✅ **Accepts:** `"Singapore Standard Time"`, `"Asia/Manila"`  
❌ **May Reject:** Some IANA IDs

```csharp
// Windows example
TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time") // Works
TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")             // May fail
```

#### Linux/Docker
✅ **Accepts:** `"Asia/Manila"`, IANA timezone IDs  
❌ **May Reject:** Windows timezone IDs  
📦 **Requires:** `tzdata` package installed

```dockerfile
# Add to Dockerfile
RUN apk add --no-cache tzdata  # For Alpine
# OR
RUN apt-get install -y tzdata  # For Debian/Ubuntu
```

### Solution

#### Check Available Timezones
```csharp
// Add this endpoint to verify timezones on your server
[HttpGet("api/schedulers/available-timezones")]
public IActionResult GetAvailableTimezones()
{
    var timezones = TimeZoneInfo.GetSystemTimeZones()
        .Select(tz => new { tz.Id, tz.DisplayName })
        .ToList();
    return Ok(timezones);
}
```

#### Recommended Timezone IDs

| Platform | Recommended ID | Alternative |
|----------|---------------|-------------|
| **Windows** | `Singapore Standard Time` | `China Standard Time` |
| **Linux/Docker** | `Asia/Manila` | `Asia/Singapore` |
| **Cross-Platform** | Use mapping table | See below |

#### Cross-Platform Timezone Mapping
```csharp
// Add this to SchedulerService
private static readonly Dictionary<string, string> TimeZoneMapping = new()
{
    { "Asia/Manila", "Singapore Standard Time" },      // Linux → Windows
    { "Singapore Standard Time", "Asia/Manila" },      // Windows → Linux
    { "Asia/Singapore", "Singapore Standard Time" }
};
```

### Validation Now Built-In
The updated code validates the timezone before creating the schedule:

```csharp
if (!_schedulerService.ValidateTimeZone(dto.TimeZone))
{
    throw new ArgumentException($"Invalid timezone: {dto.TimeZone}");
}
```

---

## 4. Hangfire Server Not Running

### Symptoms
- All schedules show `nextExecutionTime` = null
- Hangfire dashboard is empty or not accessible
- No jobs execute

### Verification
```bash
# Check Hangfire dashboard
curl https://localhost:5001/hangfire

# Check if Hangfire service is running
# Look for "Hangfire Server started" in logs
```

### Solution
```bash
# Restart the application
dotnet run --project NTC.QoS.API

# Or restart the service/container
systemctl restart ntc-qos-api  # Linux
# OR
docker restart ntc-qos-api      # Docker
```

---

## 5. Cron Expression Issues

### Problem
Invalid cron expression prevents job registration.

### Common Mistakes
```
❌ "14 10 * * *"  # Hour > 23 (uses 24-hour format)
✅ "10 14 * * *"  # Correct: Minute Hour Day Month DayOfWeek

❌ "0 2 * * 1-5"  # Monday to Friday (1-5)
✅ "0 2 * * 1,2,3,4,5"  # Correct for Cronos

❌ "0 0 32 * *"  # Day 32 doesn't exist
✅ "0 0 1 * *"   # First day of month
```

### Validation
The system now validates cron expressions during creation. Check logs for:
```
Failed to calculate next execution time for cron '...'
```

---

## Emergency Procedures

### Schedule Created But Not Running - Quick Fix

#### Step 1: Check the Schedule
```bash
GET /api/schedulers/{id}
```

Look for:
- `"nextExecutionTime": null` ← Problem
- `"status": 1` (Paused) ← Problem
- `"status": 3` (Failed) ← Problem

#### Step 2: Force Re-registration
```bash
# Pause
POST /api/schedulers/{id}/pause

# Wait 2 seconds

# Resume
POST /api/schedulers/{id}/resume
```

This forces Hangfire to re-register the job.

#### Step 3: Verify in Hangfire Dashboard
```
Navigate to: https://localhost:5001/hangfire/recurring
Look for job ID: ConnectivityAssessment_xxxxx
Check "Next execution" column
```

#### Step 4: Check Logs
```bash
# Look for these log entries
grep "Creating scheduler" application.log
grep "Registered Hangfire recurring job" application.log
grep "Next execution" application.log
```

### Manual Recovery for Missed Jobs

If the schedule missed today's execution and you need the data:

#### Option 1: Trigger One-Time Job
```bash
POST /api/analytics/connectivity-assessment/start
{
  "locationIds": ["<your-location-id>"],
  "locationType": 3,
  "generateHierarchicalAssessments": true
}
```

#### Option 2: Create Catch-Up Schedule
```bash
POST /api/schedulers
{
  "name": "Catch-up - Run in 5 minutes",
  "frequency": 5,  # Custom
  "configuration": {
    "customCronExpression": "*/5 * * * *"  # Every 5 minutes
  },
  "locationType": 3,
  "locationIds": [...],
  "timeZone": "Asia/Manila",
  "endDate": "2025-11-26T23:59:59"  # Expires today
}
```

Then delete the catch-up schedule after it runs.

---

## Monitoring & Prevention

### Health Check Endpoint
Add this to monitor scheduler health:

```csharp
[HttpGet("api/schedulers/health")]
public async Task<IActionResult> GetSchedulerHealth()
{
    var activeSchedulers = await _repository.GetActiveSchedulersAsync();
    var issues = activeSchedulers
        .Where(s => s.NextExecutionTime == null)
        .Select(s => new { s.Id, s.Name, Issue = "NextExecutionTime is null" })
        .ToList();
        
    return Ok(new { 
        TotalActive = activeSchedulers.Count(), 
        IssuesFound = issues.Count,
        Issues = issues 
    });
}
```

### Proactive Monitoring
Run this query every hour:

```sql
-- Find schedules that should have run but haven't
SELECT 
    "Id",
    "Name",
    "Status",
    "NextExecutionTime",
    "LastExecutionTime",
    "FailureCount"
FROM "ConnectivityAssessmentSchedulers"
WHERE 
    "IsDeleted" = FALSE
    AND "Status" = 0  -- Active
    AND ("NextExecutionTime" IS NULL 
         OR "NextExecutionTime" < NOW() - INTERVAL '1 hour')
    AND "CreatedAt" < NOW() - INTERVAL '10 minutes';
```

### Best Practices

1. **Create schedules at least 10 minutes before first run**
2. **Verify `nextExecutionTime` is populated after creation**
3. **Use valid timezone IDs for your platform**
4. **Monitor Hangfire dashboard regularly**
5. **Set up alerts for schedules with null nextExecutionTime**
6. **Use Pause → Update → Resume for changes near execution time**
7. **Keep Hangfire server running 24/7**
8. **Review logs after creating/updating schedules**

---

## Log Messages to Watch For

### Success
```
Creating scheduler 'Daily Region 1' with cron '0 2 * * *', TimeZone: Asia/Manila, Next run: 2025-11-27T06:00:00Z
Registered Hangfire recurring job 'ConnectivityAssessment_xxx' with cron '0 2 * * *'
Scheduler 'Daily Region 1' (ID: xxx) created and registered successfully. Next execution: 2025-11-27T06:00:00Z
```

### Warning Signs
```
❌ Invalid timezone: Asia/Manila
❌ Timezone 'Asia/Manila' not found
❌ Failed to calculate next execution time
❌ NextExecutionTime is null after creation
```

If you see any warnings, investigate immediately!
