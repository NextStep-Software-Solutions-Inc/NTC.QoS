#!/usr/bin/env python3
"""
Applies staged GeoJSON boundary files to NTC.QoS.API with automatic backup.
"""

import sys
import shutil
from pathlib import Path

def main():
    root_api = Path(r"C:\Users\almau\NTC.QoS-Backend\NTC.QoS.API")
    staging_dir = root_api / "geojson_staging"
    
    files = ["admin1.geojson", "admin2.geojson", "admin3.geojson", "admin4.geojson"]
    
    print("=" * 60)
    print("Applying Staged GeoJSON Boundary Files to NTC.QoS.API")
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

    print("\n? Successfully applied all staged GeoJSON files to NTC.QoS.API!")
    print("Original files backed up with '.bak_pre_ra12000' suffix.")

if __name__ == "__main__":
    main()
