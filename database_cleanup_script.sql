-- ============================================================================
-- PRODUCTION DATABASE CLEANUP SCRIPT
-- ============================================================================
-- Purpose: Clean up duplicate location data caused by repeated seeding
-- CRITICAL: BACKUP DATABASE BEFORE RUNNING THIS SCRIPT!
-- ============================================================================

-- ============================================================================
-- STEP 1: DIAGNOSTIC QUERIES
-- ============================================================================

-- Check database size and disk usage
SELECT 
    pg_database.datname AS database_name,
    pg_size_pretty(pg_database_size(pg_database.datname)) AS size,
    pg_database_size(pg_database.datname) AS size_bytes
FROM pg_database
WHERE pg_database.datname NOT IN ('template0', 'template1')
ORDER BY pg_database_size(pg_database.datname) DESC;

-- Check for advisory locks (should be none when app is stopped)
SELECT 
    locktype, 
    database, 
    classid, 
    objid, 
    pid, 
    mode, 
    granted,
    pg_blocking_pids(pid) as blocking_pids
FROM pg_locks 
WHERE locktype = 'advisory';

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS indexes_size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 20;

-- Count records in each location table
SELECT 'Regions' as table_name, COUNT(*) as record_count FROM "Regions"
UNION ALL
SELECT 'Provinces', COUNT(*) FROM "Provinces"
UNION ALL
SELECT 'Municipalities', COUNT(*) FROM "Municipalities"
UNION ALL
SELECT 'Barangays', COUNT(*) FROM "Barangays";

-- ============================================================================
-- STEP 2: IDENTIFY DUPLICATES
-- ============================================================================

-- Find duplicate Regions
SELECT 
    "Code", 
    COUNT(*) as duplicate_count,
    array_agg("Id" ORDER BY "CreatedAt" DESC) as all_ids,
    MIN("CreatedAt") as oldest_created,
    MAX("CreatedAt") as newest_created
FROM "Regions"
GROUP BY "Code"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- Find duplicate Provinces
SELECT 
    "Code", 
    COUNT(*) as duplicate_count,
    array_agg("Id" ORDER BY "CreatedAt" DESC) as all_ids,
    MIN("CreatedAt") as oldest_created,
    MAX("CreatedAt") as newest_created
FROM "Provinces"
GROUP BY "Code"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- Find duplicate Municipalities
SELECT 
    "Code", 
    COUNT(*) as duplicate_count,
    array_agg("Id" ORDER BY "CreatedAt" DESC) as all_ids,
    MIN("CreatedAt") as oldest_created,
    MAX("CreatedAt") as newest_created
FROM "Municipalities"
GROUP BY "Code"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- Find duplicate Barangays
SELECT 
    "Code", 
    COUNT(*) as duplicate_count,
    array_agg("Id" ORDER BY "CreatedAt" DESC) as all_ids,
    MIN("CreatedAt") as oldest_created,
    MAX("CreatedAt") as newest_created
FROM "Barangays"
GROUP BY "Code"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- ============================================================================
-- STEP 3: RELEASE STUCK ADVISORY LOCKS (if any)
-- ============================================================================

-- Release all advisory locks (run only if locks are stuck)
SELECT pg_advisory_unlock_all();

-- If specific lock is stuck, terminate the session holding it
-- First identify the PID from the locks query above, then:
-- SELECT pg_terminate_backend(PID_NUMBER_HERE);

-- ============================================================================
-- STEP 4: CHECK DATABASE READ-ONLY STATUS
-- ============================================================================

-- Check if database is in read-only mode
SHOW default_transaction_read_only;

-- Check for read-only due to disk full
-- (This might show errors if disk is full)
SELECT * FROM pg_stat_database WHERE datname = current_database();

-- ============================================================================
-- STEP 5: BACKUP BEFORE CLEANUP
-- ============================================================================

-- Run this from command line (NOT in SQL):
-- pg_dump -h HOST -U USERNAME -d DATABASE_NAME -F c -f backup_before_cleanup_$(date +%Y%m%d_%H%M%S).backup

-- Or for plain SQL format:
-- pg_dump -h HOST -U USERNAME -d DATABASE_NAME > backup_before_cleanup_$(date +%Y%m%d_%H%M%S).sql

-- ============================================================================
-- STEP 6: DELETE DUPLICATES (KEEP OLDEST RECORD)
-- ============================================================================

-- CRITICAL: Run these in a transaction so you can rollback if needed!

BEGIN;

-- Preview what will be deleted for Barangays (run this first to verify)
SELECT b1.*
FROM "Barangays" b1
WHERE b1."Id" NOT IN (
    SELECT MIN(b2."Id")
    FROM "Barangays" b2
    GROUP BY b2."Code"
)
ORDER BY b1."Code";

-- If preview looks correct, delete duplicate Barangays
DELETE FROM "Barangays" b1
WHERE b1."Id" NOT IN (
    SELECT MIN(b2."Id")
    FROM "Barangays" b2
    GROUP BY b2."Code"
);

-- Preview what will be deleted for Municipalities
SELECT m1.*
FROM "Municipalities" m1
WHERE m1."Id" NOT IN (
    SELECT MIN(m2."Id")
    FROM "Municipalities" m2
    GROUP BY m2."Code"
)
ORDER BY m1."Code";

-- Delete duplicate Municipalities
DELETE FROM "Municipalities" m1
WHERE m1."Id" NOT IN (
    SELECT MIN(m2."Id")
    FROM "Municipalities" m2
    GROUP BY m2."Code"
);

-- Preview what will be deleted for Provinces
SELECT p1.*
FROM "Provinces" p1
WHERE p1."Id" NOT IN (
    SELECT MIN(p2."Id")
    FROM "Provinces" p2
    GROUP BY p2."Code"
)
ORDER BY p1."Code";

-- Delete duplicate Provinces
DELETE FROM "Provinces" p1
WHERE p1."Id" NOT IN (
    SELECT MIN(p2."Id")
    FROM "Provinces" p2
    GROUP BY p2."Code"
);

-- Preview what will be deleted for Regions
SELECT r1.*
FROM "Regions" r1
WHERE r1."Id" NOT IN (
    SELECT MIN(r2."Id")
    FROM "Regions" r2
    GROUP BY r2."Code"
)
ORDER BY r1."Code";

-- Delete duplicate Regions
DELETE FROM "Regions" r1
WHERE r1."Id" NOT IN (
    SELECT MIN(r2."Id")
    FROM "Regions" r2
    GROUP BY r2."Code"
);

-- Review changes before committing
SELECT 'Regions' as table_name, COUNT(*) as record_count FROM "Regions"
UNION ALL
SELECT 'Provinces', COUNT(*) FROM "Provinces"
UNION ALL
SELECT 'Municipalities', COUNT(*) FROM "Municipalities"
UNION ALL
SELECT 'Barangays', COUNT(*) FROM "Barangays";

-- If everything looks good, COMMIT. Otherwise, ROLLBACK
-- COMMIT;
-- Or if something is wrong:
-- ROLLBACK;

-- ============================================================================
-- STEP 7: VERIFY CLEANUP
-- ============================================================================

-- Check for any remaining duplicates (should return 0 rows)
SELECT "Code", COUNT(*) FROM "Regions" GROUP BY "Code" HAVING COUNT(*) > 1;
SELECT "Code", COUNT(*) FROM "Provinces" GROUP BY "Code" HAVING COUNT(*) > 1;
SELECT "Code", COUNT(*) FROM "Municipalities" GROUP BY "Code" HAVING COUNT(*) > 1;
SELECT "Code", COUNT(*) FROM "Barangays" GROUP BY "Code" HAVING COUNT(*) > 1;

-- Expected counts (Philippines standard):
-- Regions: ~17
-- Provinces: ~81
-- Municipalities: ~1,500-1,700
-- Barangays: ~42,000-43,000

-- ============================================================================
-- STEP 8: RECLAIM DISK SPACE
-- ============================================================================

-- Analyze tables to update statistics
ANALYZE "Regions";
ANALYZE "Provinces";
ANALYZE "Municipalities";
ANALYZE "Barangays";

-- Vacuum tables to reclaim space (non-blocking)
VACUUM ANALYZE "Regions";
VACUUM ANALYZE "Provinces";
VACUUM ANALYZE "Municipalities";
VACUUM ANALYZE "Barangays";

-- Full vacuum to reclaim maximum space (LOCKS TABLES - run during maintenance window)
-- VACUUM FULL "Regions";
-- VACUUM FULL "Provinces";
-- VACUUM FULL "Municipalities";
-- VACUUM FULL "Barangays";

-- Reindex to rebuild indexes (optional, improves performance)
REINDEX TABLE "Regions";
REINDEX TABLE "Provinces";
REINDEX TABLE "Municipalities";
REINDEX TABLE "Barangays";

-- ============================================================================
-- STEP 9: CHECK DISK SPACE RECLAIMED
-- ============================================================================

-- Check table sizes after cleanup
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS indexes_size
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN ('Regions', 'Provinces', 'Municipalities', 'Barangays')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Check total database size
SELECT 
    pg_database.datname AS database_name,
    pg_size_pretty(pg_database_size(pg_database.datname)) AS size
FROM pg_database
WHERE pg_database.datname = current_database();

-- ============================================================================
-- STEP 10: ADD UNIQUE CONSTRAINTS (PREVENT FUTURE DUPLICATES)
-- ============================================================================

-- These should already exist, but verify:

-- Check existing unique constraints
SELECT 
    tc.constraint_name, 
    tc.table_name, 
    kcu.column_name
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
    ON tc.constraint_name = kcu.constraint_name
WHERE tc.constraint_type = 'UNIQUE'
  AND tc.table_name IN ('Regions', 'Provinces', 'Municipalities', 'Barangays')
ORDER BY tc.table_name, kcu.ordinal_position;

-- If unique constraints are missing, add them:
-- ALTER TABLE "Regions" ADD CONSTRAINT "UQ_Regions_Code" UNIQUE ("Code");
-- ALTER TABLE "Provinces" ADD CONSTRAINT "UQ_Provinces_Code" UNIQUE ("Code");
-- ALTER TABLE "Municipalities" ADD CONSTRAINT "UQ_Municipalities_Code" UNIQUE ("Code");
-- ALTER TABLE "Barangays" ADD CONSTRAINT "UQ_Barangays_Code" UNIQUE ("Code");

-- ============================================================================
-- EXECUTION CHECKLIST
-- ============================================================================

-- Before running this script:
-- [ ] Stop the application to prevent new inserts
-- [ ] Backup the database
-- [ ] Verify you have enough disk space for the cleanup operation
-- [ ] Test on a copy/staging environment first if possible

-- Execution order:
-- [ ] Step 1: Run diagnostics to understand the problem
-- [ ] Step 2: Identify duplicates and their counts
-- [ ] Step 3: Release any stuck advisory locks
-- [ ] Step 4: Check read-only status
-- [ ] Step 5: BACKUP DATABASE (CRITICAL!)
-- [ ] Step 6: Delete duplicates in a transaction (Barangays ? Municipalities ? Provinces ? Regions)
-- [ ] Step 7: Verify cleanup was successful
-- [ ] Step 8: Reclaim disk space with VACUUM
-- [ ] Step 9: Verify disk space reclaimed
-- [ ] Step 10: Ensure unique constraints exist

-- After cleanup:
-- [ ] Deploy code fixes (advisory lock + ForceReseed=false)
-- [ ] Start application
-- [ ] Monitor logs for proper seeding behavior
-- [ ] Monitor database size for 1 week

-- ============================================================================
-- NOTES
-- ============================================================================

-- • Keep oldest record (MIN("Id")) to preserve original data
-- • Delete in reverse hierarchy order to avoid foreign key violations
-- • Use transactions to allow rollback if needed
-- • VACUUM FULL reclaims more space but locks tables - use during maintenance
-- • Monitor disk usage after cleanup to ensure problem doesn't recur

-- Expected deletion counts (if seeded N times):
-- Regions: ~17 × (N-1) duplicates
-- Provinces: ~81 × (N-1) duplicates  
-- Municipalities: ~1,500 × (N-1) duplicates
-- Barangays: ~42,000 × (N-1) duplicates

-- If seeded 10 times:
-- Total duplicates ? (17 + 81 + 1,500 + 42,000) × 9 ? 393,000 records!
-- With geometry data, this could be 10s of GBs

-- ============================================================================
