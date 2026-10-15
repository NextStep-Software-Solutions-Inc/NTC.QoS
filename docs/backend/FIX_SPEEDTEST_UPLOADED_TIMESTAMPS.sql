-- ==============================================================================
-- Remediation Script: Fix File-Uploaded Speedtest Timestamps (PHT -> UTC Correction)
-- ==============================================================================
-- Issue:
--   Prior to the fix, Speedtest Excel files logged in Philippine Standard Time (UTC+8)
--   were parsed as DateTimeKind.Unspecified and converted via .ToUniversalTime() on a
--   UTC host container. This caused local PHT timestamps (e.g. 04:12 PM) to be stored
--   as 04:12 PM UTC instead of 08:12 AM UTC.
--   When displayed in the web UI (browser in UTC+8), the time was offset by +8 hours
--   (e.g., displaying 12:12 AM the next day).
--
-- Scope:
--   Only records uploaded from files ("IsFileUploaded" = true) are affected.
--   Manual entries created via UI ("IsFileUploaded" = false) are NOT affected because
--   the web browser converts manual selections to UTC ISO strings prior to submission.
-- ==============================================================================

BEGIN;

-- 1. Preview count of records to be corrected
SELECT COUNT(*) AS affected_uploaded_speedtests
FROM "Speedtests"
WHERE "IsFileUploaded" = true;

-- 2. Adjust DateTime by subtracting 8 hours to restore true UTC
UPDATE "Speedtests"
SET "DateTime" = "DateTime" - INTERVAL '8 hours',
    "UpdatedAt" = NOW()
WHERE "IsFileUploaded" = true;

-- 3. Verify updated sample
SELECT "Id", "Address", "DateTime", "UploadSpeed", "DownloadSpeed"
FROM "Speedtests"
WHERE "IsFileUploaded" = true
ORDER BY "DateTime" DESC
LIMIT 10;

COMMIT;
