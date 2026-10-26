#!/usr/bin/env python3
"""
Applies staged GeoJSON boundary files to NTC.QoS.API with automatic backup.
Dynamically resolves paths relative to the repository root for cross-platform compatibility.
"""

import sys
import shutil
import argparse
from pathlib import Path

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
        if orig.exists() and not bak.exists():
            print(f"Backing up: {orig.name} -> {bak.name}...")
            shutil.copy2(orig, bak)
        elif bak.exists():
            print(f"Backup {bak.name} already exists (skipping overwrite).")

    # 3. Copy staged into root_api
    for f in files:
        staged_file = staging_dir / f
        target_file = root_api / f
        print(f"Copying: {staged_file.name} -> {target_file}...")
        shutil.copy2(staged_file, target_file)

    print("\n✔ Successfully applied all staged GeoJSON files to NTC.QoS.API!")
    print("Original files backed up with '.bak_pre_ra12000' suffix.")

if __name__ == "__main__":
    main()
