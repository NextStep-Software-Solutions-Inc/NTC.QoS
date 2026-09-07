# Production Database Issue Investigation Report

## ?? Executive Summary

**Issue**: Production database disk full due to duplicate location data being seeded every night.
**Root Cause**: Multiple critical bugs in advisory lock implementation + incorrect production configuration.
**Impact**: Database in READ-ONLY mode, unable to delete duplicate data.

---

## ?? Issues Identified

### 1. **CRITICAL: Advisory Lock Never Acquired** ??

**Location**: `NTC.QoS.Infrastructure\InfrastructureDependencyInjection.cs`

**Problem**: The code declares `lockAcquired = false` but never actually acquires the lock:

```csharp
// BEFORE (BROKEN):
var lockAcquired = false;  // ? Always false!

try
{
    // ? NO CODE TO ACQUIRE THE LOCK!
    await SeedInitialDataAsync(...);
}
finally
{
    if (lockAcquired)  // ? This never executes because lockAcquired is always false
        await ReleaseAdvisoryLockAsync(...);
}
```

**Impact**: 
- Multiple application instances can seed simultaneously
- No protection against concurrent seeding operations
- Advisory lock is never released (but was never acquired either)

**Fix Applied**:
```csharp
// AFTER (FIXED):
var lockAcquired = false;

try
{
    // ? Actually acquire the lock
    lockAcquired = await TryAcquireAdvisoryLockAsync(dbContext, advisoryLockKey, logger, cancellationToken);
    
    if (!lockAcquired)
    {
        logger.LogWarning("Could not acquire advisory lock. Another instance may be seeding. Skipping.");
        return;
    }

    await SeedInitialDataAsync(...);
}
finally
{
    if (lockAcquired)
        await ReleaseAdvisoryLockAsync(...);
}
```

---

### 2. **CRITICAL: ForceReseed = TRUE in Production** ??

**Location**: `NTC.QoS.API\appsettings.Production.json`

**Problem**:
```json
{
  "Database": {
    "ForceReseed": true  // ? WRONG for production!
  }
}
```

**Impact**:
- Bypasses ALL duplicate checks
- Every app restart inserts duplicate location data
- With ~42,000+ barangays × multiple tables = massive disk usage

**Fix**: Set to `false` in production:
```json
{
  "Database": {
    "ForceReseed": false  // ? CORRECT for production
  }
}
```

---

### 3. **Compounding Factor: RunMigrationsOnStartup = TRUE**

**Location**: `NTC.QoS.API\appsettings.Production.json`

**Problem**: Combined with `ForceReseed=true`, this runs seeding on EVERY restart:
- Container restarts
- Deployments
- Health check failures
- Scaling events
- **Scheduled nightly restarts** ? This is why it happens "every night"

**Recommendation**: 
- Keep `RunMigrationsOnStartup: true` for migrations
- But ensure `ForceReseed: false` to prevent re-seeding

---

### 4. **PostgreSQL Advisory Lock Behavior**

**Session-based locks**: PostgreSQL advisory locks are **session-based** by default:
- Lock is automatically released when the database connection closes
- If app crashes without releasing, lock is auto-released on connection close
- **However**, if connection pool keeps connections alive, lock may persist

**Potential Issue**: If the application crashes during seeding:
1. Advisory lock remains held by the connection
2. Connection might stay in pool if not properly closed
3. Next seeding attempt cannot acquire lock
4. But if lock acquisition code is missing (Issue #1), this doesn't matter anyway

---

## ?? What Was Happening Every Night

1. **Scheduled restart** (or deployment/health check recovery)
2. App starts ? `ApplyMigrationsAndSeedAsync()` called
3. Advisory lock acquisition code **missing** ? no lock protection
4. `ForceReseed = true` ? bypasses duplicate checks
5. **All location data inserted again**:
   - ~17 regions
   - ~81 provinces  
   - ~1,500+ municipalities
   - ~42,000+ barangays
   - Each with MultiPolygon geometry data (LARGE!)
6. Disk fills up progressively
7. PostgreSQL triggers READ-ONLY mode
8. Unable to delete because READ-ONLY
9. **Cycle repeats next night** ? disk fills more

---

## ? Fixes Applied

### Fix 1: Implemented Advisory Lock Acquisition

**File**: `NTC.QoS.Infrastructure\InfrastructureDependencyInjection.cs`

**Changes**:
1. Added `TryAcquireAdvisoryLockAsync()` method
2. Actually acquire lock before seeding
3. Skip seeding if lock cannot be acquired
4. Improved logging at all stages

### Fix 2: Enhanced Defensive Checks

**File**: `NTC.QoS.Infrastructure\Seed\LocationSeeder.cs`

**Changes**:
1. Added warning when `ForceReseed=true` but data exists
2. Maintains individual duplicate checks in each seeder method
3. Better logging to track seeding behavior

### Fix 3: Created Safe Production Configuration

**File**: `NTC.QoS.API\appsettings.Production.SAFE.json` (NEW)

**Changes**:
```json
{
  "Database": {
    "ForceReseed": false  // ? Safe for production
  }
}
```

---

## ?? Immediate Action Required

### Step 1: Fix Database Read-Only State

```sql
-- Check current database state
SELECT pg_database.datname, pg_database_size(pg_database.datname) 
FROM pg_database;

-- Check for any held advisory locks
SELECT locktype, database, classid, objid, pid, mode, granted 
FROM pg_locks 
WHERE locktype = 'advisory';

-- Release any stuck advisory locks (if found)
SELECT pg_advisory_unlock_all();

-- If database is in read-only mode due to disk full:
-- 1. Free up disk space on server
-- 2. Restart PostgreSQL service
-- 3. Verify database is writable
```

### Step 2: Clean Up Duplicate Data

**?? BACKUP DATABASE FIRST!**

```sql
-- Count duplicates before cleanup
SELECT code, COUNT(*) 
FROM "Regions" 
GROUP BY code 
HAVING COUNT(*) > 1;

SELECT code, COUNT(*) 
FROM "Provinces" 
GROUP BY code 
HAVING COUNT(*) > 1;

-- Delete duplicates, keeping oldest record
-- Example for Regions:
DELETE FROM "Regions" r1
WHERE r1."Id" NOT IN (
    SELECT MIN(r2."Id")
    FROM "Regions" r2
    GROUP BY r2."Code"
);

-- Repeat for Provinces, Municipalities, Barangays
```

### Step 3: Update Production Configuration

Replace current `appsettings.Production.json` with the safe version:

```bash
# Backup current config
cp appsettings.Production.json appsettings.Production.json.backup

# Update with safe config
cp appsettings.Production.SAFE.json appsettings.Production.json
```

### Step 4: Deploy Fixed Code

1. Deploy the updated code with advisory lock fix
2. Verify logs show lock acquisition
3. Monitor disk usage

---

## ?? Verification Steps

After deploying fixes:

### 1. Check Logs for Advisory Lock

Look for these log messages:
```
? "Attempting to acquire advisory lock 987654321012345678..."
? "Advisory lock 987654321012345678 acquired successfully"
? "Location data already exists in database. Skipping seeding process."
? "Advisory lock 987654321012345678 released successfully"
```

### 2. Monitor Database Size

```sql
-- Track database growth
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### 3. Check for Duplicate Records

```sql
-- Should return 0 duplicates
SELECT code, COUNT(*) as count
FROM "Regions"
GROUP BY code
HAVING COUNT(*) > 1;

SELECT code, COUNT(*) as count
FROM "Provinces"
GROUP BY code
HAVING COUNT(*) > 1;

SELECT code, COUNT(*) as count
FROM "Municipalities"
GROUP BY code
HAVING COUNT(*) > 1;

SELECT code, COUNT(*) as count
FROM "Barangays"
GROUP BY code
HAVING COUNT(*) > 1;
```

---

## ?? Best Practices for Future

### 1. Configuration Management

**Development**:
```json
{
  "Database": {
    "RunMigrationsOnStartup": true,
    "ForceReseed": false  // Let duplicate checks work
  }
}
```

**Production**:
```json
{
  "Database": {
    "RunMigrationsOnStartup": true,   // OK for migrations
    "ForceReseed": false               // NEVER true in production!
  }
}
```

**When to use ForceReseed=true**:
- **NEVER in production** unless explicitly fixing data
- Only in development for testing
- Only when you intentionally want to bypass duplicate checks
- Always with database backup

### 2. Advisory Lock Pattern

The fix implements the proper pattern:

```csharp
var lockAcquired = false;
try
{
    // Acquire lock
    lockAcquired = await TryAcquireAdvisoryLockAsync(...);
    if (!lockAcquired) return;  // Fail fast
    
    // Do protected work
    await SeedData();
}
finally
{
    // Always release in finally block
    if (lockAcquired)
        await ReleaseAdvisoryLockAsync(...);
}
```

### 3. Seeding Strategy

**Current approach** (with fixes):
- Uses advisory lock for concurrency control
- Duplicate checks in each seeder method
- Fail-safe even if ForceReseed=true

**Alternative approach** (for consideration):
- Separate seeding from migrations
- Run seeding as one-time migration
- Use database migration history to track seeding

### 4. Monitoring

Add alerts for:
- Database size growth > X% per day
- Advisory lock acquisition failures
- Duplicate seeding detected
- Disk usage > 80%

---

## ?? PostgreSQL Advisory Locks Reference

### Session-based locks (used in this fix):
```sql
SELECT pg_try_advisory_lock(key);   -- Acquire (non-blocking)
SELECT pg_advisory_lock(key);       -- Acquire (blocking)
SELECT pg_advisory_unlock(key);     -- Release specific lock
SELECT pg_advisory_unlock_all();    -- Release all locks
```

### Transaction-based locks:
```sql
SELECT pg_try_advisory_xact_lock(key);  -- Auto-release at transaction end
```

**Key differences**:
- Session locks: Manual release required, survive transaction commits
- Transaction locks: Auto-release at transaction end

**Our implementation uses session locks** because:
- Seeding spans multiple transactions
- Need explicit control over lock lifetime
- Want lock to survive individual insert operations

---

## ?? Related Files Modified

1. `NTC.QoS.Infrastructure\InfrastructureDependencyInjection.cs` - Fixed advisory lock
2. `NTC.QoS.Infrastructure\Seed\LocationSeeder.cs` - Enhanced warnings
3. `NTC.QoS.API\appsettings.Production.SAFE.json` - Safe production config

---

## ?? Timeline of Events (Hypothesis)

1. **Initial deployment**: Data seeded correctly once
2. **First issue**: Advisory lock never acquired (code bug)
3. **Second issue**: ForceReseed=true set in production (config error)
4. **Nightly restarts**: Scheduled maintenance or health checks
5. **Each restart**: Duplicates all location data
6. **After N nights**: Disk full ? READ-ONLY mode
7. **Current state**: Can't delete, can't insert, can only read

---

## ? Resolution Checklist

- [x] Identify root causes
- [x] Implement advisory lock acquisition
- [x] Add defensive warnings
- [x] Create safe production config
- [ ] Free disk space on server
- [ ] Restore database writability
- [ ] Delete duplicate records
- [ ] Deploy fixed code
- [ ] Update production configuration
- [ ] Verify no more duplicates
- [ ] Monitor for 1 week
- [ ] Add disk usage alerts

---

**Report Generated**: 2025-01-XX
**Severity**: Critical
**Status**: Code fixed, awaiting deployment and database cleanup
