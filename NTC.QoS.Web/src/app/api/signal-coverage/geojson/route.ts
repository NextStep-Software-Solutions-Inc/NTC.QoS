import { promises as fs } from "node:fs";
import path from "node:path";
import { NextResponse } from "next/server";

type RawGeoJSONFeature = {
  type?: string;
  geometry?: unknown;
  properties?: Record<string, unknown>;
};

type RawGeoJSONFeatureCollection = {
  type?: string;
  features?: RawGeoJSONFeature[];
};

export const runtime = "nodejs";

const DATA_DIR = path.join(process.cwd(), "public", "data");

const deriveAreaStudyLabel = (sourceFile: string) => {
  const withoutExtension = sourceFile.replace(/\.geojson$/i, "");
  return withoutExtension.replace(/\s-\s[0-9a-f-]{36}$/i, "").trim();
};

async function collectGeoJSONFiles(dir: string): Promise<string[]> {
  const entries = await fs.readdir(dir, { withFileTypes: true });
  const files = await Promise.all(
    entries.map(async (entry) => {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        return collectGeoJSONFiles(fullPath);
      }

      if (entry.isFile() && entry.name.toLowerCase().endsWith(".geojson")) {
        return [fullPath];
      }

      return [];
    }),
  );

  return files.flat();
}

export async function GET() {
  try {
    const geojsonFiles = await collectGeoJSONFiles(DATA_DIR);
    const mergedFeatures: RawGeoJSONFeature[] = [];

    for (const filePath of geojsonFiles) {
      try {
        const raw = await fs.readFile(filePath, "utf-8");
        const parsed = JSON.parse(raw) as RawGeoJSONFeatureCollection;

        if (parsed.type !== "FeatureCollection" || !Array.isArray(parsed.features)) {
          continue;
        }

        const sourceFile = path.basename(filePath);
        const areaStudy = deriveAreaStudyLabel(sourceFile);

        for (const feature of parsed.features) {
          if (!feature || feature.type !== "Feature") continue;

          mergedFeatures.push({
            ...feature,
            properties: {
              ...(feature.properties ?? {}),
              areaStudy,
              source_file: sourceFile,
            },
          });
        }
      } catch {
        // Skip invalid/unreadable files and continue processing other files.
      }
    }

    return NextResponse.json({
      type: "FeatureCollection",
      features: mergedFeatures,
      fileCount: geojsonFiles.length,
      featureCount: mergedFeatures.length,
    });
  } catch (error) {
    return NextResponse.json(
      {
        error: "Failed to read GeoJSON files from public/data",
        detail: error instanceof Error ? error.message : "Unknown error",
      },
      { status: 500 },
    );
  }
}
