-- ==============================================================================
-- Republic Act No. 12000 (Negros Island Region Act) Database Migration
-- 
-- Non-destructive in-place migration for PostgreSQL / PostGIS:
-- 1. Inserts NIR (Negros Island Region) with code 'PH18'.
-- 2. Updates Province foreign keys: Negros Occidental, Negros Oriental, Siquijor -> NIR.
-- 3. Recalculates Region VII (Central Visayas) boundary = Cebu + Bohol.
-- 4. Recalculates Region VI (Western Visayas) boundary = excluding Negros Occidental.
-- ==============================================================================

-- Ensure PostGIS extension is enabled before executing spatial operations
CREATE EXTENSION IF NOT EXISTS postgis;

BEGIN;

DO $$
DECLARE
    -- Candidate deterministic UUID for NIR
    v_nir_id_candidate UUID := '018f7000-0000-7000-8000-000000000018'::uuid;
    v_nir_id UUID;
    v_r7_id UUID;
    v_r6_id UUID;
    v_now TIMESTAMPTZ := clock_timestamp();
    v_system_user TEXT := 'System';
BEGIN
    -- Verify PostGIS extension is active
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'postgis') THEN
        RAISE EXCEPTION 'PostGIS extension is not installed or enabled in target database. Please ensure PostGIS is enabled before running spatial migrations.';
    END IF;
    SELECT "Id" INTO v_r7_id FROM "Regions" WHERE "Code" = 'PH07' LIMIT 1;
    SELECT "Id" INTO v_r6_id FROM "Regions" WHERE "Code" = 'PH06' LIMIT 1;

    IF v_r7_id IS NULL THEN
        RAISE EXCEPTION 'Region VII (Code PH07) not found in Regions table!';
    END IF;

    IF v_r6_id IS NULL THEN
        RAISE EXCEPTION 'Region VI (Code PH06) not found in Regions table!';
    END IF;

    -- Check if NIR already exists by code
    SELECT "Id" INTO v_nir_id FROM "Regions" WHERE "Code" = 'PH18' LIMIT 1;

    -- 1. Insert NIR into Regions table using PostGIS union of its constituent provinces
    IF v_nir_id IS NULL THEN
        -- Collision check: ensure candidate UUID does not collide with an existing row in Regions
        IF EXISTS (SELECT 1 FROM "Regions" WHERE "Id" = v_nir_id_candidate) THEN
            v_nir_id := gen_random_uuid();
            RAISE NOTICE 'Candidate UUID % already in use; generated dynamic UUID: %', v_nir_id_candidate, v_nir_id;
        ELSE
            v_nir_id := v_nir_id_candidate;
        END IF;

        INSERT INTO "Regions" (
            "Id", "Name", "Code", "Description", "Area",
            "TotalPopulation", "HouseholdPopulation", "HouseholdCount",
            "Boundaries", "Center", "IsDeleted",
            "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
        )
        SELECT 
            v_nir_id,
            'Negros Island Region (NIR)',
            'PH18',
            'Created under Republic Act No. 12000',
            COALESCE(SUM(p."Area"), 0),
            COALESCE(SUM(p."TotalPopulation"), 0),
            COALESCE(SUM(p."HouseholdPopulation"), 0),
            COALESCE(SUM(p."HouseholdCount"), 0),
            ST_Multi(ST_Union(p."Boundaries")),
            ST_Centroid(ST_Union(p."Boundaries")),
            FALSE,
            v_now,
            v_system_user,
            v_now,
            v_system_user
        FROM "Provinces" p
        WHERE p."Code" IN ('PH06045', 'PH07046', 'PH07061');
        
        RAISE NOTICE 'Inserted NIR (PH18) with ID: %', v_nir_id;
    ELSE
        RAISE NOTICE 'NIR (PH18) already exists with ID: %', v_nir_id;
    END IF;

    -- 2. Update Provinces: Point Negros Occidental, Negros Oriental, and Siquijor to NIR
    UPDATE "Provinces"
    SET "RegionId" = v_nir_id,
        "UpdatedAt" = v_now,
        "UpdatedBy" = v_system_user
    WHERE "Code" IN ('PH06045', 'PH07046', 'PH07061');
    
    RAISE NOTICE 'Updated 3 provinces (Negros Occidental, Negros Oriental, Siquijor) to Region NIR.';

    -- 3. Update Region VII (Central Visayas) - Now only Cebu and Bohol
    UPDATE "Regions" r
    SET 
        "Area" = sub.total_area,
        "TotalPopulation" = sub.total_pop,
        "HouseholdPopulation" = sub.hhold_pop,
        "HouseholdCount" = sub.hhold_cnt,
        "Boundaries" = sub.new_geom,
        "Center" = ST_Centroid(sub.new_geom),
        "UpdatedAt" = v_now,
        "UpdatedBy" = v_system_user
    FROM (
        SELECT 
            COALESCE(SUM(p."Area"), 0) AS total_area,
            COALESCE(SUM(p."TotalPopulation"), 0) AS total_pop,
            COALESCE(SUM(p."HouseholdPopulation"), 0) AS hhold_pop,
            COALESCE(SUM(p."HouseholdCount"), 0) AS hhold_cnt,
            ST_Multi(ST_Union(p."Boundaries")) AS new_geom
        FROM "Provinces" p
        WHERE p."Code" IN ('PH07012', 'PH07022') -- Bohol, Cebu
    ) sub
    WHERE r."Id" = v_r7_id;
    
    RAISE NOTICE 'Updated Region VII boundary and metrics (Bohol, Cebu).';

    -- 4. Update Region VI (Western Visayas) - Excluding Negros Occidental
    UPDATE "Regions" r
    SET 
        "Area" = sub.total_area,
        "TotalPopulation" = sub.total_pop,
        "HouseholdPopulation" = sub.hhold_pop,
        "HouseholdCount" = sub.hhold_cnt,
        "Boundaries" = sub.new_geom,
        "Center" = ST_Centroid(sub.new_geom),
        "UpdatedAt" = v_now,
        "UpdatedBy" = v_system_user
    FROM (
        SELECT 
            COALESCE(SUM(p."Area"), 0) AS total_area,
            COALESCE(SUM(p."TotalPopulation"), 0) AS total_pop,
            COALESCE(SUM(p."HouseholdPopulation"), 0) AS hhold_pop,
            COALESCE(SUM(p."HouseholdCount"), 0) AS hhold_cnt,
            ST_Multi(ST_Union(p."Boundaries")) AS new_geom
        FROM "Provinces" p
        WHERE p."RegionId" = v_r6_id AND p."Code" NOT IN ('PH06045')
    ) sub
    WHERE r."Id" = v_r6_id;

    RAISE NOTICE 'Updated Region VI boundary and metrics (Panay provinces + Guimaras).';

END $$;

COMMIT;

-- ==============================================================================
-- Verification Queries
-- ==============================================================================
-- 1. Province counts per region (Expected: PH06 = 5, PH07 = 2, PH18 = 3)
SELECT r."Code", r."Name", COUNT(p."Id") AS "ProvinceCount"
FROM "Regions" r
LEFT JOIN "Provinces" p ON p."RegionId" = r."Id" AND NOT p."IsDeleted"
WHERE r."Code" IN ('PH06', 'PH07', 'PH18')
GROUP BY r."Code", r."Name"
ORDER BY r."Code";

-- 2. Verify geometry validity
SELECT "Code", "Name", ST_IsValid("Boundaries") AS "GeometryIsValid", ST_GeometryType("Boundaries") AS "GeomType"
FROM "Regions"
WHERE "Code" IN ('PH06', 'PH07', 'PH18')
ORDER BY "Code";
