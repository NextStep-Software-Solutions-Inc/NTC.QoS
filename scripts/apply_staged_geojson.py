#!/usr/bin/env python3
"""
Applies staged GeoJSON boundary files to NTC.QoS.API with automatic backup.
Dynamically resolves paths relative to the repository root for cross-platform compatibility.
"""

import sys
import shutil
import argparse
import datetime
from pathlib import Path


def is_already_migrated_or_staged(orig: Path, staged: Path) -> bool:
    """
    Check if the target file is already migrated or identical to staged.
    Prevents creating a pre-migration backup from an already-migrated file.
    """
    if not orig.exists():
        return False

    # Check 1: File size matches staged file and initial chunk matches
    if staged.exists() and orig.stat().st_size == staged.stat().st_size:
        try:
            with open(orig, "rb") as f1, open(staged, "rb") as f2:
                if f1.read(65536) == f2.read(65536):
                    return True
        except Exception:
            pass

    # Check 2: Content scan for RA 12000 NIR markers (PH18 / Negros Island Region)
    try:
        with open(orig, "r", encoding="utf-8", errors="ignore") as f:
            for _ in range(5000):
                line = f.readline()
                if not line:
                    break
                if '"PH18"' in line or "Negros Island Region" in line or '"ADM1_PCODE":"PH18"' in line:
                    return True
    except Exception:
        pass

    return False


def main():
    repo_root = Path(__file__).resolve().parent.parent
    default_api_dir = repo_root / "NTC.QoS.API"

    parser = argparse.ArgumentParser(description="Applies staged GeoJSON boundary files to NTC.QoS.API with backup.")
    parser.add_argument(
        "--api-dir",
        type=Path,
        default=default_api_dir,
        help="Path to NTC.QoS.API directory (defaults to <repo_root>/NTC.QoS.API)."
    )
    parser.add_argument(
        "--staging-dir",
        type=Path,
        default=None,
        help="Path to staging directory (defaults to <api-dir>/geojson_staging)."
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Simulate execution without modifying or copying any files."
    )
    parser.add_argument(
        "--log-file",
        type=Path,
        default=None,
        help="Optional path to write structured audit log output."
    )
    args = parser.parse_args()

    root_api = args.api_dir.resolve()
    staging_dir = (args.staging_dir or (root_api / "geojson_staging")).resolve()
    log_handle = open(args.log_file, "a", encoding="utf-8") if args.log_file else None

    def audit_log(msg: str):
        timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        prefix = f"[{timestamp}] " if not msg.startswith("=") else ""
        formatted = f"{prefix}{msg}"
        print(formatted)
        if log_handle:
            log_handle.write(formatted + "\n")
            log_handle.flush()

    files = ["admin1.geojson", "admin2.geojson", "admin3.geojson", "admin4.geojson"]
    
    audit_log("=" * 60)
    audit_log("Applying Staged GeoJSON Boundary Files to NTC.QoS.API")
    audit_log(f"Execution Mode:    {'DRY-RUN (Simulated)' if args.dry_run else 'LIVE'}")
    audit_log(f"Target Directory:  {root_api}")
    audit_log(f"Staging Directory: {staging_dir}")
    if args.log_file:
        audit_log(f"Audit Log File:    {args.log_file.resolve()}")
    audit_log("=" * 60)
    
    # 1. Verify all staged files exist
    for f in files:
        staged_file = staging_dir / f
        if not staged_file.exists():
            audit_log(f"Error: Staged file {staged_file} does not exist!")
            if log_handle:
                log_handle.close()
            sys.exit(1)
            
    # 2. Backup existing originals
    for f in files:
        orig = root_api / f
        bak = root_api / f"{f}.bak_pre_ra12000"
        staged_file = staging_dir / f

        if bak.exists():
            audit_log(f"Backup {bak.name} already exists (skipping overwrite).")
        elif orig.exists():
            if is_already_migrated_or_staged(orig, staged_file):
                audit_log(f"Notice: {orig.name} is already migrated or matches staged file. Skipping backup to protect pre-migration baseline.")
            else:
                orig_size_mb = orig.stat().st_size / (1024 * 1024)
                if args.dry_run:
                    audit_log(f"[DRY-RUN] Would backup: {orig.name} ({orig_size_mb:.2f} MB) -> {bak.name}")
                else:
                    audit_log(f"Backing up: {orig.name} ({orig_size_mb:.2f} MB) -> {bak.name}...")
                    shutil.copy2(orig, bak)

    # 3. Copy staged into root_api
    for f in files:
        staged_file = staging_dir / f
        target_file = root_api / f
        staged_size_mb = staged_file.stat().st_size / (1024 * 1024)

        if target_file.exists() and target_file.stat().st_size == staged_file.stat().st_size:
            try:
                with open(target_file, "rb") as f1, open(staged_file, "rb") as f2:
                    if f1.read(65536) == f2.read(65536):
                        audit_log(f"Skipping copy: {target_file.name} is already up to date with staged file.")
                        continue
            except Exception:
                pass

        if args.dry_run:
            audit_log(f"[DRY-RUN] Would copy: {staged_file.name} ({staged_size_mb:.2f} MB) -> {target_file.name}")
        else:
            audit_log(f"Copying: {staged_file.name} ({staged_size_mb:.2f} MB) -> {target_file}...")
            shutil.copy2(staged_file, target_file)

    if args.dry_run:
        audit_log("\n[SUCCESS] [DRY-RUN] Simulation completed successfully. No files were modified on disk.")
    else:
        audit_log("\n[SUCCESS] Successfully applied all staged GeoJSON files to NTC.QoS.API!")
        audit_log("Original files backed up with '.bak_pre_ra12000' suffix.")

    if log_handle:
        log_handle.close()

if __name__ == "__main__":
    main()
