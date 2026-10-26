#!/usr/bin/env python3
"""
Republic Act No. 12000 - GeoJSON Boundary Transformation Pipeline
Updates Philippine administrative boundaries (levels 1-4) to reflect the
creation of the Negros Island Region (NIR).

Transfers:
- Negros Occidental (PH06045) from Region VI to NIR (PH18)
- Negros Oriental (PH07046) from Region VII to NIR (PH18)
- Siquijor (PH07061) from Region VII to NIR (PH18)

Resulting Regions:
- Region VII: Bohol, Cebu (2 provinces)
- Region VI: Aklan, Antique, Capiz, Guimaras, Iloilo (5 provinces)
- NIR (PH18): Negros Occidental, Negros Oriental, Siquijor (3 provinces)
"""

import os
import sys
import json
import time
import shutil
import argparse
import re
from pathlib import Path
from shapely.geometry import shape, mapping, MultiPolygon, Polygon
from shapely.ops import unary_union

# Target constants
NIR_PCODE = "PH18"
NIR_NAME = "Negros Island Region (NIR)"
NIR_ALT_NAME = "Negros Island Region"
RA12000_DATE = "2024-06-11"

NIR_PROVINCE_CODES = {
    "PH06045": "Negros Occidental",
    "PH07046": "Negros Oriental",
    "PH07061": "Siquijor"
}

R7_REMAINING_CODES = {
    "PH07012": "Bohol",
    "PH07022": "Cebu"
}

R6_REMAINING_CODES = {
    "PH06004": "Aklan",
    "PH06006": "Antique",
    "PH06019": "Capiz",
    "PH06030": "Iloilo",
    "PH06079": "Guimaras"
}

ALL_WATCHED_PROVINCES = {
    **NIR_PROVINCE_CODES,
    **R7_REMAINING_CODES,
    **R6_REMAINING_CODES
}


def to_multipolygon(geom):
    """Ensure geometry is a MultiPolygon."""
    if isinstance(geom, Polygon):
        return MultiPolygon([geom])
    elif isinstance(geom, MultiPolygon):
        return geom
    else:
        polys = [p for p in getattr(geom, 'geoms', []) if isinstance(p, (Polygon, MultiPolygon))]
        flat_polys = []
        for p in polys:
            if isinstance(p, Polygon):
                flat_polys.append(p)
            elif isinstance(p, MultiPolygon):
                flat_polys.extend(p.geoms)
        return MultiPolygon(flat_polys)


def process_admin2(src_path: Path, dst_path: Path):
    """
    Process admin2.geojson (Provinces):
    1. Extracts geometries for Region VI, Region VII, and NIR constituent provinces.
    2. Updates ADM1_PCODE and ADM1_EN for Negros Occidental, Negros Oriental, Siquijor.
    3. Streams output to dst_path with zero geometry drift.
    """
    print(f"\n[1/4] Processing admin2: {src_path.name} -> {dst_path.name}...")
    t0 = time.time()
    
    extracted_geoms = {}
    extracted_props = {}
    updated_count = 0
    total_count = 0

    with open(src_path, "r", encoding="utf-8") as f_in, \
         open(dst_path, "w", encoding="utf-8") as f_out:
        
        for line in f_in:
            line_s = line.strip()
            # Non-feature lines (header/footer)
            if not line_s or line_s in ('{', '}', '],', ']}') or line_s.startswith('"'):
                f_out.write(line)
                continue
            
            total_count += 1
            has_trailing_comma = line.rstrip("\r\n").endswith(",")
            feat_raw = line.rstrip(",\r\n")
            
            # Check if this feature is one of our watched provinces
            matched_code = None
            for pcode in ALL_WATCHED_PROVINCES:
                if f'"ADM2_PCODE": "{pcode}"' in line or f'"ADM2_PCODE":"{pcode}"' in line:
                    matched_code = pcode
                    break
            
            if matched_code:
                feat = json.loads(feat_raw)
                extracted_geoms[matched_code] = shape(feat["geometry"])
                extracted_props[matched_code] = dict(feat["properties"])
                
                # If it's one of the 3 provinces transferring to NIR, update ADM1
                if matched_code in NIR_PROVINCE_CODES:
                    feat["properties"]["ADM1_PCODE"] = NIR_PCODE
                    feat["properties"]["ADM1_EN"] = NIR_NAME
                    feat["properties"]["validOn"] = RA12000_DATE
                    updated_count += 1
                    
                    # Write updated feature line
                    out_line = json.dumps(feat, ensure_ascii=False)
                    if has_trailing_comma:
                        out_line += ",\n"
                    else:
                        out_line += "\n"
                    f_out.write(out_line)
                    continue

            # Write line as-is
            f_out.write(line)

    print(f"  Processed {total_count} provinces ({updated_count} transferred to NIR) in {time.time() - t0:.2f}s")
    return extracted_geoms, extracted_props


def process_admin1(src_path: Path, dst_path: Path, geoms: dict, props: dict):
    """
    Process admin1.geojson (Regions):
    1. Computes NIR boundary = unary_union(NIR provinces).
    2. Computes Region VII boundary = unary_union(Bohol, Cebu).
    3. Computes Region VI boundary = unary_union(Aklan, Antique, Capiz, Iloilo, Guimaras).
    4. Updates R6, R7 in-place, and appends NIR feature.
    """
    print(f"\n[2/4] Processing admin1: {src_path.name} -> {dst_path.name}...")
    t0 = time.time()
    
    # 1. Compute Unions
    print("  Calculating geometric unions using Shapely...")
    nir_union = to_multipolygon(unary_union([geoms[c] for c in NIR_PROVINCE_CODES]))
    r7_union = to_multipolygon(unary_union([geoms[c] for c in R7_REMAINING_CODES]))
    r6_union = to_multipolygon(unary_union([geoms[c] for c in R6_REMAINING_CODES]))
    
    nir_area_sqkm = sum(props[c]["AREA_SQKM"] for c in NIR_PROVINCE_CODES)
    r7_area_sqkm = sum(props[c]["AREA_SQKM"] for c in R7_REMAINING_CODES)
    r6_area_sqkm = sum(props[c]["AREA_SQKM"] for c in R6_REMAINING_CODES)
    
    print(f"  NIR: valid={nir_union.is_valid}, area={nir_area_sqkm:.2f} sq km, polygons={len(nir_union.geoms)}")
    print(f"  Region VII: valid={r7_union.is_valid}, area={r7_area_sqkm:.2f} sq km, polygons={len(r7_union.geoms)}")
    print(f"  Region VI: valid={r6_union.is_valid}, area={r6_area_sqkm:.2f} sq km, polygons={len(r6_union.geoms)}")
    
    # Build NIR Feature
    nir_feature = {
        "type": "Feature",
        "properties": {
            "ADM1_EN": NIR_NAME,
            "ADM1_PCODE": NIR_PCODE,
            "ADM0_EN": "Philippines (the)",
            "ADM0_PCODE": "PH",
            "date": RA12000_DATE,
            "validOn": RA12000_DATE,
            "validTo": None,
            "Shape_Leng": float(nir_union.length),
            "Shape_Area": float(nir_union.area),
            "ADM1ALT1EN": NIR_ALT_NAME,
            "AREA_SQKM": float(nir_area_sqkm)
        },
        "geometry": mapping(nir_union)
    }

    feature_lines = []
    header_lines = []
    footer_lines = []
    
    with open(src_path, "r", encoding="utf-8") as f_in:
        in_features = False
        in_footer = False
        
        for line in f_in:
            line_s = line.strip()
            
            # Header state
            if not in_features and not in_footer:
                header_lines.append(line)
                if line_s.endswith('"features": ['):
                    in_features = True
                continue
            
            # Features state
            if in_features:
                if line_s == "]" or line_s == "]}" or line_s.startswith("]"):
                    in_features = False
                    in_footer = True
                    footer_lines.append(line)
                    continue
                    
                if line_s:
                    feat_raw = line.rstrip(",\r\n")
                    feat = json.loads(feat_raw)
                    pcode = feat["properties"].get("ADM1_PCODE")
                    
                    if pcode == "PH07":
                        feat["geometry"] = mapping(r7_union)
                        feat["properties"]["AREA_SQKM"] = float(r7_area_sqkm)
                        feat["properties"]["Shape_Leng"] = float(r7_union.length)
                        feat["properties"]["Shape_Area"] = float(r7_union.area)
                        feat["properties"]["validOn"] = RA12000_DATE
                    elif pcode == "PH06":
                        feat["geometry"] = mapping(r6_union)
                        feat["properties"]["AREA_SQKM"] = float(r6_area_sqkm)
                        feat["properties"]["Shape_Leng"] = float(r6_union.length)
                        feat["properties"]["Shape_Area"] = float(r6_union.area)
                        feat["properties"]["validOn"] = RA12000_DATE
                    
                    feature_lines.append(feat)
                continue
                
            # Footer state
            if in_footer:
                footer_lines.append(line)
                continue

    # Append NIR feature
    feature_lines.append(nir_feature)
    
    # Write output
    with open(dst_path, "w", encoding="utf-8") as f_out:
        for hl in header_lines:
            f_out.write(hl)
            
        for i, feat in enumerate(feature_lines):
            line_str = json.dumps(feat, ensure_ascii=False)
            if i < len(feature_lines) - 1:
                f_out.write(line_str + ",\n")
            else:
                f_out.write(line_str + "\n")
                
        for fl in footer_lines:
            f_out.write(fl)

    print(f"  Processed {len(feature_lines)} regions (including NIR) in {time.time() - t0:.2f}s")


def process_admin_streaming(src_path: Path, dst_path: Path, admin_level: int):
    """
    Process admin3 or admin4:
    Streams lines directly; replaces ADM1_PCODE and ADM1_EN in properties for
    any feature belonging to Negros Occidental, Negros Oriental, or Siquijor.
    Zero geometry deserialization = 100% coordinate precision preserved.
    """
    print(f"\n[{admin_level}/4] Processing admin{admin_level}: {src_path.name} -> {dst_path.name}...")
    t0 = time.time()
    
    target_provinces = set(NIR_PROVINCE_CODES.keys())
    updated_count = 0
    total_count = 0

    with open(src_path, "r", encoding="utf-8") as f_in, \
         open(dst_path, "w", encoding="utf-8") as f_out:
        
        for line in f_in:
            line_s = line.strip()
            if not line_s or line_s in ('{', '}', '],', ']}') or line_s.startswith('"'):
                f_out.write(line)
                continue
                
            total_count += 1
            has_match = False
            for pcode in target_provinces:
                if f'"ADM2_PCODE": "{pcode}"' in line or f'"ADM2_PCODE":"{pcode}"' in line:
                    has_match = True
                    break
                    
            if has_match:
                # Patch ADM1 properties
                new_line = re.sub(r'"ADM1_PCODE"\s*:\s*"PH0[67]"', f'"ADM1_PCODE": "{NIR_PCODE}"', line)
                new_line = re.sub(r'"ADM1_EN"\s*:\s*"Region (VI|VII) \([^"]+\)"', f'"ADM1_EN": "{NIR_NAME}"', new_line)
                f_out.write(new_line)
                updated_count += 1
            else:
                f_out.write(line)

    print(f"  Processed {total_count} admin{admin_level} features ({updated_count} patched to NIR) in {time.time() - t0:.2f}s")


def validate_staged_files(staging_dir: Path):
    """
    Automated verification of transformed files in staging using lightweight streaming.
    """
    print(f"\n[Validation] Verifying staged GeoJSON integrity in {staging_dir}...")
    
    # 1. Admin 1 Validation
    a1_path = staging_dir / "admin1.geojson"
    assert a1_path.exists(), "admin1.geojson missing!"
    regions = {}
    with open(a1_path, "r", encoding="utf-8") as f:
        for line in f:
            if '"ADM1_PCODE"' in line:
                feat = json.loads(line.rstrip(',\r\n'))
                pcode = feat["properties"]["ADM1_PCODE"]
                regions[pcode] = feat
    
    assert len(regions) == 18, f"Expected 18 regions, found {len(regions)}"
    assert "PH18" in regions, "PH18 (NIR) missing from admin1!"
    assert "PH07" in regions, "PH07 missing from admin1!"
    assert "PH06" in regions, "PH06 missing from admin1!"
    
    # Verify NIR geometry
    nir_shape = shape(regions["PH18"]["geometry"])
    assert nir_shape.is_valid, "NIR geometry is invalid!"
    print(f"  ? Admin 1: 18 regions verified. NIR geometry is valid ({nir_shape.geom_type}).")

    # 2. Admin 2 Validation
    a2_path = staging_dir / "admin2.geojson"
    assert a2_path.exists(), "admin2.geojson missing!"
    nir_provs = []
    r7_provs = []
    r6_provs = []
    with open(a2_path, "r", encoding="utf-8") as f:
        for line in f:
            if '"ADM2_PCODE"' in line:
                for pcode in NIR_PROVINCE_CODES:
                    if f'"ADM2_PCODE": "{pcode}"' in line or f'"ADM2_PCODE":"{pcode}"' in line:
                        assert f'"ADM1_PCODE": "{NIR_PCODE}"' in line, f"{pcode} did not have ADM1_PCODE: PH18"
                        nir_provs.append(pcode)
                for pcode in R7_REMAINING_CODES:
                    if f'"ADM2_PCODE": "{pcode}"' in line or f'"ADM2_PCODE":"{pcode}"' in line:
                        assert '"ADM1_PCODE": "PH07"' in line
                        r7_provs.append(pcode)
                for pcode in R6_REMAINING_CODES:
                    if f'"ADM2_PCODE": "{pcode}"' in line or f'"ADM2_PCODE":"{pcode}"' in line:
                        assert '"ADM1_PCODE": "PH06"' in line
                        r6_provs.append(pcode)
                        
    assert len(nir_provs) == 3, f"Expected 3 NIR provinces, found {len(nir_provs)}"
    assert len(r7_provs) == 2, f"Expected 2 Region VII provinces, found {len(r7_provs)}"
    assert len(r6_provs) == 5, f"Expected 5 Region VI provinces, found {len(r6_provs)}"
    print(f"  ? Admin 2: Province distribution verified (NIR: 3, Region VII: 2, Region VI: 5).")

    # 3. Admin 3 Validation
    a3_path = staging_dir / "admin3.geojson"
    assert a3_path.exists(), "admin3.geojson missing!"
    a3_nir_count = 0
    with open(a3_path, "r", encoding="utf-8") as f:
        for line in f:
            if f'"ADM1_PCODE": "{NIR_PCODE}"' in line:
                a3_nir_count += 1
    assert a3_nir_count == 63, f"Expected 63 municipalities in NIR, found {a3_nir_count}"
    print(f"  ? Admin 3: Exactly 63 municipalities/cities verified in NIR.")

    # 4. Admin 4 Validation
    a4_path = staging_dir / "admin4.geojson"
    assert a4_path.exists(), "admin4.geojson missing!"
    a4_nir_count = 0
    with open(a4_path, "r", encoding="utf-8") as f:
        for line in f:
            if f'"ADM1_PCODE": "{NIR_PCODE}"' in line:
                a4_nir_count += 1
    assert a4_nir_count == 1353, f"Expected 1,353 barangays in NIR, found {a4_nir_count}"
    print(f"  ? Admin 4: Exactly 1,353 barangays verified in NIR.")

    print("\n?? All validation tests passed successfully!")


def main():
    repo_root = Path(__file__).resolve().parent.parent
    default_api_dir = repo_root / "NTC.QoS.API"

    parser = argparse.ArgumentParser(description="RA 12000 Administrative Boundaries Transformation Pipeline")
    parser.add_argument(
        "--api-dir",
        type=Path,
        default=default_api_dir,
        help="Path to NTC.QoS.API directory containing source GeoJSON files."
    )
    parser.add_argument(
        "--staging-dir",
        type=Path,
        default=None,
        help="Path to staging output directory (defaults to <api-dir>/geojson_staging)."
    )
    args = parser.parse_args()

    root_api = args.api_dir.resolve()
    staging_dir = (args.staging_dir or (root_api / "geojson_staging")).resolve()
    staging_dir.mkdir(parents=True, exist_ok=True)
    
    print("=" * 70)
    print("RA 12000 Administrative Boundaries Transformation Pipeline")
    print(f"Source Directory:  {root_api}")
    print(f"Staging Directory: {staging_dir}")
    print("=" * 70)
    
    total_start = time.time()
    
    # Step 1: Process admin2
    src_admin2 = root_api / "admin2.geojson"
    dst_admin2 = staging_dir / "admin2.geojson"
    geoms, props = process_admin2(src_admin2, dst_admin2)
    
    # Step 2: Process admin1
    src_admin1 = root_api / "admin1.geojson"
    dst_admin1 = staging_dir / "admin1.geojson"
    process_admin1(src_admin1, dst_admin1, geoms, props)
    
    # Step 3: Process admin3
    src_admin3 = root_api / "admin3.geojson"
    dst_admin3 = staging_dir / "admin3.geojson"
    process_admin_streaming(src_admin3, dst_admin3, admin_level=3)
    
    # Step 4: Process admin4
    src_admin4 = root_api / "admin4.geojson"
    dst_admin4 = staging_dir / "admin4.geojson"
    process_admin_streaming(src_admin4, dst_admin4, admin_level=4)
    
    # Step 5: Validate all staged files
    validate_staged_files(staging_dir)
    
    print("\n" + "=" * 70)
    print(f"? Transformation & Validation completed in {time.time() - total_start:.2f}s")
    print(f"Staged files are ready at: {staging_dir}")
    print("=" * 70)


if __name__ == "__main__":
    main()
