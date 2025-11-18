-- =====================================================================================
-- CellSector Without Cellular/Sector Records - Cleanup Guide
-- =====================================================================================
-- Purpose: Identify and optionally clean up CellSectors that were created without
--          any Cellular or Sector records (typically from Excel files with all
--          technology/band columns set to FALSE).
--
-- Impact:  These orphaned CellSectors cannot be used for coverage analysis and
--          occupy database space without providing value.
--
-- Created: 2024
-- =====================================================================================

-- =====================================================================================
-- SECTION 1: DIAGNOSTIC QUERIES
-- =====================================================================================

-- Query 1: Find CellSectors WITHOUT any Cellular or Sector records
-- This identifies CellSectors that are "orphaned" - they exist but have no coverage data
SELECT 
    cs."Id" as CellSectorId,
    c."Id" as CellId,
    c."Name" as CellName,
    c."ServiceProviderId",
    p."Name" as ProviderName,
    cs."AntennaType",
    cs."Azimuth",
    cs."BeamTilt",
    cs."AntennaHeight",
    c."CreatedAt",
    c."CreatedBy"
FROM "CellSectors" cs
INNER JOIN "Cells" c ON cs."CellId" = c."Id"
INNER JOIN "ServiceProviders" sp ON c."ServiceProviderId" = sp."Id"
INNER JOIN "Providers" p ON sp."ProviderId" = p."Id"
LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
WHERE cell."Id" IS NULL 
  AND s."Id" IS NULL
  AND c."IsFileUploaded" = true  -- Only file-uploaded cells
ORDER BY c."CreatedAt" DESC;

-- Query 2: Count orphaned CellSectors by Provider
SELECT 
    p."Name" as ProviderName,
    COUNT(DISTINCT cs."Id") as OrphanedCellSectorCount,
    COUNT(DISTINCT c."Id") as AffectedCellCount,
    MIN(c."CreatedAt") as OldestOrphan,
    MAX(c."CreatedAt") as NewestOrphan
FROM "CellSectors" cs
INNER JOIN "Cells" c ON cs."CellId" = c."Id"
INNER JOIN "ServiceProviders" sp ON c."ServiceProviderId" = sp."Id"
INNER JOIN "Providers" p ON sp."ProviderId" = p."Id"
LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
WHERE cell."Id" IS NULL 
  AND s."Id" IS NULL
  AND c."IsFileUploaded" = true
GROUP BY p."Name"
ORDER BY OrphanedCellSectorCount DESC;

-- Query 3: Find Cells where ALL CellSectors are orphaned (no coverage data at all)
SELECT 
    c."Id" as CellId,
    c."Name" as CellName,
    p."Name" as ProviderName,
    COUNT(cs."Id") as TotalCellSectors,
    COUNT(DISTINCT cell."Id") as CellularsCount,
    COUNT(DISTINCT s."Id") as SectorsCount,
    c."CreatedAt",
    c."Location"
FROM "Cells" c
INNER JOIN "ServiceProviders" sp ON c."ServiceProviderId" = sp."Id"
INNER JOIN "Providers" p ON sp."ProviderId" = p."Id"
INNER JOIN "CellSectors" cs ON c."Id" = cs."CellId"
LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
WHERE c."IsFileUploaded" = true
GROUP BY c."Id", c."Name", p."Name", c."CreatedAt", c."Location"
HAVING COUNT(DISTINCT cell."Id") = 0 
   AND COUNT(DISTINCT s."Id") = 0
ORDER BY c."CreatedAt" DESC;

-- Query 4: Summary statistics
SELECT 
    'Total CellSectors' as Metric,
    COUNT(*) as Count
FROM "CellSectors"
UNION ALL
SELECT 
    'CellSectors with Cellular records' as Metric,
    COUNT(DISTINCT cs."Id") as Count
FROM "CellSectors" cs
INNER JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
UNION ALL
SELECT 
    'CellSectors with Sector records' as Metric,
    COUNT(DISTINCT cs."Id") as Count
FROM "CellSectors" cs
INNER JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
UNION ALL
SELECT 
    'Orphaned CellSectors (no Cellular or Sector)' as Metric,
    COUNT(DISTINCT cs."Id") as Count
FROM "CellSectors" cs
LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
WHERE cell."Id" IS NULL AND s."Id" IS NULL;

-- =====================================================================================
-- SECTION 2: CLEANUP OPTIONS
-- =====================================================================================

-- ??  WARNING: These are destructive operations. Always backup your database first!
-- ??  Review the diagnostic queries above before running any cleanup.

-- Option A: Delete orphaned CellSectors only (keeps the Cell)
-- Use this when you want to preserve the Cell for future uploads with valid data
/*
BEGIN TRANSACTION;

DELETE FROM "CellSectors"
WHERE "Id" IN (
    SELECT cs."Id"
    FROM "CellSectors" cs
    LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
    LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
    INNER JOIN "Cells" c ON cs."CellId" = c."Id"
    WHERE cell."Id" IS NULL 
      AND s."Id" IS NULL
      AND c."IsFileUploaded" = true
      AND c."CreatedAt" < NOW() - INTERVAL '30 days'  -- Safety: only old orphans
);

-- Review the deletion count
SELECT * FROM "CellSectors" WHERE "Id" NOT IN (SELECT "Id" FROM "CellSectors");

-- COMMIT; -- Uncomment to commit the transaction
ROLLBACK; -- Remove this line when ready to commit
*/

-- Option B: Delete Cells AND orphaned CellSectors (full cleanup)
-- Use this when Cells with no coverage data should be completely removed
/*
BEGIN TRANSACTION;

-- First, find Cells where ALL CellSectors are orphaned
WITH OrphanedCells AS (
    SELECT c."Id" as CellId
    FROM "Cells" c
    INNER JOIN "CellSectors" cs ON c."Id" = cs."CellId"
    LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
    LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
    WHERE c."IsFileUploaded" = true
      AND c."CreatedAt" < NOW() - INTERVAL '30 days'  -- Safety: only old orphans
    GROUP BY c."Id"
    HAVING COUNT(DISTINCT cell."Id") = 0 
       AND COUNT(DISTINCT s."Id") = 0
)
-- Delete CellSectors first (due to foreign key)
DELETE FROM "CellSectors"
WHERE "CellId" IN (SELECT CellId FROM OrphanedCells);

-- Then delete the Cells
DELETE FROM "Cells"
WHERE "Id" IN (SELECT CellId FROM OrphanedCells);

-- Review the deletion count
SELECT 'Deleted Cells' as Operation, COUNT(*) FROM OrphanedCells;

-- COMMIT; -- Uncomment to commit the transaction
ROLLBACK; -- Remove this line when ready to commit
*/

-- Option C: Mark for review (non-destructive)
-- Add a comment or flag to orphaned CellSectors for manual review
/*
UPDATE "CellSectors"
SET "UpdatedAt" = NOW(),
    "UpdatedBy" = 'System-OrphanedFlagged'
WHERE "Id" IN (
    SELECT cs."Id"
    FROM "CellSectors" cs
    LEFT JOIN "Cellular" cell ON cs."Id" = cell."CellSectorId"
    LEFT JOIN "Sectors" s ON cs."Id" = s."CellSectorId"
    INNER JOIN "Cells" c ON cs."CellId" = c."Id"
    WHERE cell."Id" IS NULL 
      AND s."Id" IS NULL
      AND c."IsFileUploaded" = true
);
*/

-- =====================================================================================
-- SECTION 3: POST-CLEANUP VALIDATION
-- =====================================================================================

-- Verify database integrity after cleanup
SELECT 
    'Cells without CellSectors' as Issue,
    COUNT(*) as Count
FROM "Cells" c
LEFT JOIN "CellSectors" cs ON c."Id" = cs."CellId"
WHERE cs."Id" IS NULL
UNION ALL
SELECT 
    'CellSectors without Cells' as Issue,
    COUNT(*) as Count
FROM "CellSectors" cs
LEFT JOIN "Cells" c ON cs."CellId" = c."Id"
WHERE c."Id" IS NULL
UNION ALL
SELECT 
    'Sectors without CellSectors' as Issue,
    COUNT(*) as Count
FROM "Sectors" s
LEFT JOIN "CellSectors" cs ON s."CellSectorId" = cs."Id"
WHERE cs."Id" IS NULL
UNION ALL
SELECT 
    'Cellular without CellSectors' as Issue,
    COUNT(*) as Count
FROM "Cellular" cell
LEFT JOIN "CellSectors" cs ON cell."CellSectorId" = cs."Id"
WHERE cs."Id" IS NULL;

-- =====================================================================================
-- SECTION 4: PREVENTION RECOMMENDATIONS
-- =====================================================================================

/*
To prevent future orphaned CellSectors:

1. VALIDATE EXCEL FILES BEFORE UPLOAD
   - Ensure at least one technology/band column has TRUE or valid "CF: x, BW: y" values
   - The ProcessCellJob now provides pre-validation warnings for this

2. USER TRAINING
   - Educate users about the importance of technology/band data
   - Provide Excel template with example values

3. APPLICATION-LEVEL VALIDATION
   - Consider adding front-end validation to warn users before upload
   - Show preview of what will be created before processing

4. REGULAR MONITORING
   - Run Query 4 (Summary statistics) weekly to identify orphaned records
   - Set up alerts when orphaned percentage exceeds threshold (e.g., >5%)

5. AUTOMATED CLEANUP
   - Schedule cleanup jobs to remove orphans older than X days
   - Send reports to administrators before automated cleanup

6. DATA QUALITY DASHBOARD
   - Create a dashboard showing:
     * Total CellSectors
     * Orphaned CellSectors by Provider
     * Trend of orphaned records over time
*/
