#!/usr/bin/env python3
"""
Applies staged GeoJSON boundary files to NTC.QoS.API with automatic backup.
Dynamically resolves paths relative to the repository root for cross-platform compatibility.
"""

import sys
import shutil
import argparse
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
    args = parser.parse_args()

    root_api = args.api_dir.resolve()
    staging_dir = (args.staging_dir or (root_api / "geojson_staging")).resolve()
    
    files = ["admin1.geojson", "admin2.geojson", "admin3.geojson", "admin4.geojson"]
    
    print("=" * 60)
    print("Applying Staged GeoJSON Boundary Files to NTC.QoS.API")
    print(f"Target Directory:  {root_api}")
    print(f"Staging Directory: {staging_dir}")
    print("=" * 60)
    
    # 1. Verify all staged files exist
    for f in files:
        staged_file = staging_dir / f
        if not staged_file.exists():
            print(f"Error: Staged file {staged_file} does not exist!")
            sys.exit(1)
            
    # 2. Backup existing originals
    for f in files:
        orig = root_api / f
        bak = root_api / f"{f}.bak_pre_ra12000"
        staged_file = staging_dir / f

        if bak.exists():
            print(f"Backup {bak.name} already exists (skipping overwrite).")
        elif orig.exists():
            if is_already_migrated_or_staged(orig, staged_file):
                print(f"Notice: {orig.name} is already migrated or matches staged file. Skipping backup to protect pre-migration baseline.")
            else:
                print(f"Backing up: {orig.name} -> {bak.name}...")
                shutil.copy2(orig, bak)

    # 3. Copy staged into root_api
    for f in files:
        staged_file = staging_dir / f
        target_file = root_api / f

        if target_file.exists() and target_file.stat().st_size == staged_file.stat().st_size:
            try:
                with open(target_file, "rb") as f1, open(staged_file, "rb") as f2:
                    if f1.read(65536) == f2.read(65536):
                        print(f"Skipping copy: {target_file.name} is already up to date.")
                        continue
            except Exception:
                pass

        print(f"Copying: {staged_file.name} -> {target_file}...")
        shutil.copy2(staged_file, target_file)

    print("\n✔ Successfully applied all staged GeoJSON files to NTC.QoS.API!")
    print("Original files backed up with '.bak_pre_ra12000' suffix.")

if __name__ == "__main__":
    main()
