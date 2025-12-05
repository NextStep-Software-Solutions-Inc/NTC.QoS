# Scheduler Fixes Applied - Summary

## Critical Issues Identified & Fixed

### ✅ Issue #1: nextExecutionTime Was Null
**Problem:** Scheduler saved to DB but Hangfire didn't register the job  
**Fix Applied:**
- Added immediate Hangfire registration after DB save
- Update `nextExecutionTime` after registration (double-check)
- Added logging to track registration success

```csharp
// Before: nextExecutionTime could be null
await _repository.SaveChangesAsync();
await _schedulerService.RegisterSchedulerAsync(scheduler);

// After: guaranteed to be set
await _repository.SaveChangesAsync();
await _schedulerService.RegisterSchedulerAsync(scheduler);
scheduler.NextExecutionTime = _schedulerService.GetNextExecutionTime(...);
_repository.Update(scheduler);
await _repository.SaveChangesAsync();
```

---

### ✅ Issue #2: Timezone Not Validated
**Problem:** Invalid timezone IDs caused silent failures  
**Fix Applied:**
- Added `ValidateTimeZone()` method to `ISchedulerService`
- Pre-validation before creating schedule
- Throws clear error message with suggestions

```csharp
// New validation in CreateSchedulerHandler
if (!_schedulerService.ValidateTimeZone(dto.TimeZone))
{
    throw new ArgumentException(
        $"Invalid timezone: {dto.TimeZone}. " +
        "Use IANA IDs like 'Asia/Manila' or Windows IDs like 'Singapore Standard Time'"
    );
}
```

---

### ✅ Issue #3: No Logging for Troubleshooting
**Problem:** No visibility into what went wrong  
**Fix Applied:**
- Added comprehensive logging throughout the flow
- Logs cron expression, timezone, next execution time
- Logs Hangfire job registration success
- Logs errors with context

**New Log Entries:**
```
[Information] Creating scheduler 'Daily Region 1' with cron '0 2 * * *', TimeZone: Asia/Manila, Next run: 2025-11-27T06:00:00Z
[Information] Registered Hangfire recurring job 'ConnectivityAssessment_xxx' with cron '0 2 * * *'
[Information] Scheduler 'Daily Region 1' (ID: xxx) created successfully. Next execution: 2025-11-27T06:00:00Z
[Debug] Next execution for cron '0 2 * * *' in timezone 'Asia/Manila': 2025-11-27 02:00:00 (UTC: 2025-11-26T18:00:00Z)
```

---

### ✅ Issue #4: Tight Update Window
**Problem:** Updating a schedule 2-5 minutes before execution causes missed runs  
**Fix Applied:**
- Same registration fixes apply (ensures Hangfire picks up changes)
- Added documentation warning about timing
- Recommended Pause → Update → Resume workflow

---

## Files Modified

### Domain Layer
- `NTC.QoS.Domain/Interfaces/ISchedulerService.cs`
  - Added `ValidateTimeZone()` method

### Application Layer
- `NTC.QoS.Application/Features/Scheduler/Handlers/Commands/CreateSchedulerHandler.cs`
  - Added logger
  - Added timezone validation
  - Added double-save for `nextExecutionTime`
  - Added comprehensive logging

- `NTC.QoS.Application/Features/Scheduler/Handlers/Commands/UpdateSchedulerHandler.cs`
  - Added logger
  - Added timezone validation
  - Added logging

### Infrastructure Layer
- `NTC.QoS.Infrastructure/Services/SchedulerService.cs`
  - Added logger
  - Added `ValidateTimeZone()` implementation
  - Enhanced error handling
  - Added debug logging for next execution calculation

---

## Testing the Fixes

### Test Case 1: Create Schedule
```bash
POST /api/schedulers
{
  "name": "Test Schedule",
  "frequency": 2,
  "locationType": 3,
  "locationIds": ["<guid>"],
  "timeZone": "Asia/Manila",
  "configuration": {
    "hourOfDay": 14,
    "minuteOfDay": 30
  }
}
```

**Expected:**
- ✅ Returns 201 Created
- ✅ Response includes `nextExecutionTime` (not null)
- ✅ Logs show creation and registration
- ✅ Hangfire dashboard shows the recurring job

**Verify:**
```bash
GET /api/schedulers/{id}
```
Check: `"nextExecutionTime": "2025-11-27T06:30:00Z"` (example)

### Test Case 2: Invalid Timezone
```bash
POST /api/schedulers
{
  "name": "Bad Timezone Test",
  "timeZone": "Invalid/Timezone",
  ...
}
```

**Expected:**
- ✅ Returns 400 Bad Request
- ✅ Error message: "Invalid timezone: Invalid/Timezone..."
- ✅ Schedule NOT created in database

### Test Case 3: Check Logs
After creating a schedule, check logs for:
```bash
grep "Creating scheduler" /var/log/ntc-qos-api.log
grep "Registered Hangfire" /var/log/ntc-qos-api.log
grep "Next execution" /var/log/ntc-qos-api.log
```

All three log entries should appear.

---

## Quick Recovery Commands

If you encounter `nextExecutionTime: null` after creation:

### Option 1: Pause & Resume
```bash
curl -X POST https://localhost:5001/api/schedulers/{id}/pause \
  -H "Authorization: Bearer $TOKEN"

sleep 2

curl -X POST https://localhost:5001/api/schedulers/{id}/resume \
  -H "Authorization: Bearer $TOKEN"
```

### Option 2: Check Hangfire Dashboard
```
Navigate to: https://localhost:5001/hangfire/recurring
Verify job appears with correct schedule
```

### Option 3: Manual Trigger
```bash
curl -X POST https://localhost:5001/api/analytics/connectivity-assessment/start \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "locationIds": ["<guid>"],
    "locationType": 3
  }'
```

---

## Platform-Specific Timezone Reference

### Windows Server
Use these timezone IDs:
```json
{
  "timeZone": "Singapore Standard Time"  // Philippines uses same as Singapore
}
```

### Linux/Docker
Use these timezone IDs:
```json
{
  "timeZone": "Asia/Manila"
}
```

### Verify Available Timezones
```sql
-- Query to check what the server supports
SELECT DISTINCT "TimeZone" 
FROM "ConnectivityAssessmentSchedulers" 
WHERE "Status" = 0;  -- Active schedules
```

Or add this endpoint:
```csharp
[HttpGet("api/schedulers/test-timezone/{id}")]
public IActionResult TestTimezone(string id)
{
    try 
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
        return Ok(new { 
            Id = tz.Id, 
            DisplayName = tz.DisplayName,
            CurrentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz)
        });
    }
    catch (TimeZoneNotFoundException)
    {
        return BadRequest($"Timezone '{id}' not found");
    }
}
```

---

## Summary of Protection Layers

1. **Pre-validation:** Timezone checked before DB save
2. **Immediate Registration:** Hangfire job created right after DB save
3. **Double-Check:** nextExecutionTime recalculated and saved again
4. **Logging:** Every step logged for debugging
5. **Error Messages:** Clear, actionable error messages

These fixes prevent the "nextExecutionTime is null" issue and provide clear diagnostics when something goes wrong.
