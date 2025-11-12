# Production Database Issue - Quick Reference Guide

## ?? CRITICAL ISSUES FOUND

### 1. Advisory Lock Never Acquired
**Location**: `InfrastructureDependencyInjection.cs`
- Code declared `lockAcquired = false` but never set it to `true`
- No actual lock acquisition code existed
- Lock was never released (because it was never acquired)

### 2. ForceReseed = TRUE in Production
**Location**: `appsettings.Production.json`
- Bypasses all duplicate checks
- Causes re-seeding on every app restart
- Combined with nightly restarts = nightly duplicates

### 3. Result
- Every restart inserts ~44,000 location records again
- With geometry data = massive disk usage
- Disk full ? Database READ-ONLY
- Can't delete because READ-ONLY
- Vicious cycle

---

## ? FIXES APPLIED

### Code Changes
1. ? `InfrastructureDependencyInjection.cs` - Implemented actual advisory lock acquisition
2. ? `LocationSeeder.cs` - Added defensive warnings when ForceReseed=true
3. ? Build verified - No compilation errors

### Configuration Changes
1. ? Created `appsettings.Production.SAFE.json` with `ForceReseed: false`

### Documentation
1. ? `PRODUCTION_DATABASE_ISSUE_REPORT.md` - Full analysis and resolution guide
2. ? `database_cleanup_script.sql` - Complete SQL cleanup scripts

---

## ?? IMMEDIATE ACTION REQUIRED

### Phase 1: Database Recovery (DO FIRST!)

```bash
# 1. Free up disk space on the server
# 2. Restart PostgreSQL to restore writability
sudo systemctl restart postgresql

# 3. Verify database is writable
psql -d your_database -c "CREATE TABLE test_write (id int); DROP TABLE test_write;"
```

### Phase 2: Data Cleanup

```bash
# 1. BACKUP FIRST! (CRITICAL!)
pg_dump -h HOST -U USER -d DATABASE -F c -f backup_$(date +%Y%m%d).backup

# 2. Run the cleanup script
psql -h HOST -U USER -d DATABASE -f database_cleanup_script.sql

# Follow the script step-by-step, don't run all at once!
```

### Phase 3: Deploy Fixes

```bash
# 1. Update production config
cp appsettings.Production.SAFE.json appsettings.Production.json

# 2. Deploy the fixed code
# 3. Restart application
```

---

## ?? VERIFICATION

### Check Logs After Deployment

Look for these messages:
```
? "Attempting to acquire advisory lock..."
? "Advisory lock acquired successfully"
? "Location data already exists. Skipping seeding."
? "Advisory lock released successfully"
```

### Monitor Database

```sql
-- Should have NO duplicates
SELECT code, COUNT(*) FROM "Regions" GROUP BY code HAVING COUNT(*) > 1;
SELECT code, COUNT(*) FROM "Provinces" GROUP BY code HAVING COUNT(*) > 1;
SELECT code, COUNT(*) FROM "Municipalities" GROUP BY code HAVING COUNT(*) > 1;
SELECT code, COUNT(*) FROM "Barangays" GROUP BY code HAVING COUNT(*) > 1;

-- Expected counts
SELECT 'Regions' as table, COUNT(*) as count FROM "Regions"        -- ~17
UNION ALL
SELECT 'Provinces', COUNT(*) FROM "Provinces"                      -- ~81
UNION ALL
SELECT 'Municipalities', COUNT(*) FROM "Municipalities"            -- ~1,500
UNION ALL
SELECT 'Barangays', COUNT(*) FROM "Barangays";                     -- ~42,000
```

---

## ?? CONFIGURATION REFERENCE

### ? CORRECT Production Config

```json
{
  "Database": {
    "RunMigrationsOnStartup": true,    // OK - runs migrations
    "ForceReseed": false,               // MUST BE FALSE!
    "AdvisoryLockKey": 987654321012345678
  }
}
```

### ? WRONG Production Config (Current)

```json
{
  "Database": {
    "ForceReseed": true  // ? CAUSES THE PROBLEM!
  }
}
```

---

## ?? ROOT CAUSE ANALYSIS

### Why It Happens Every Night

1. Scheduled restart/deployment at night
2. App starts ? seeding triggered
3. Advisory lock not acquired (code bug)
4. `ForceReseed=true` bypasses checks
5. All data inserted again
6. Disk fills more
7. Repeat next night

### Why Database is READ-ONLY

1. Disk fills to capacity
2. PostgreSQL protects data integrity
3. Switches to READ-ONLY mode
4. Prevents further writes
5. Can't delete duplicates

---

## ?? FILES TO REVIEW

### Code Changes
- `NTC.QoS.Infrastructure\InfrastructureDependencyInjection.cs`
- `NTC.QoS.Infrastructure\Seed\LocationSeeder.cs`

### Configuration
- `NTC.QoS.API\appsettings.Production.json` (needs update!)
- `NTC.QoS.API\appsettings.Production.SAFE.json` (use this!)

### Documentation
- `PRODUCTION_DATABASE_ISSUE_REPORT.md` (detailed analysis)
- `database_cleanup_script.sql` (cleanup queries)
- `QUICK_REFERENCE.md` (this file)

---

## ?? SUCCESS CRITERIA

After fixes are deployed and database is cleaned:

- [ ] No advisory lock errors in logs
- [ ] "Skipping seeding" message on every restart
- [ ] Zero duplicate records in database
- [ ] Database size stable (not growing)
- [ ] Disk usage < 80%
- [ ] No READ-ONLY errors

---

## ?? ESCALATION

If issues persist after applying fixes:

1. Check application logs for errors
2. Verify PostgreSQL version supports `pg_try_advisory_lock`
3. Check for connection pooling issues
4. Review any custom database middleware
5. Consider transaction-based locks instead (`pg_try_advisory_xact_lock`)

---

## ?? Key PostgreSQL Commands

```sql
-- Check advisory locks
SELECT * FROM pg_locks WHERE locktype = 'advisory';

-- Release all advisory locks
SELECT pg_advisory_unlock_all();

-- Check database size
SELECT pg_size_pretty(pg_database_size(current_database()));

-- Check table sizes
SELECT tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename))
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---

**Last Updated**: 2025-01-XX
**Status**: Code fixed, awaiting production deployment
**Priority**: CRITICAL
