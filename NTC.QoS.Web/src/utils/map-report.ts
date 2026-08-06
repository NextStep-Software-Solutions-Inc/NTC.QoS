import type {
  BroadbandInfoData,
  MunicipalityInfo,
  ServiceProviderInfo,
} from "@/components/features/analytics/types/BroadbandInfo";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface FiberCoverageData {
  region: string;
  province: string;
  municipality: string;
  barangay: string;
  zipcode: string;
  providers: string[];
}

export interface GeocodingResult {
  address?: string;
  place?: string;
  municipality?: string;
  region?: string;
  postal_code?: string;
  country?: string;
}

export interface AnalysisBounds {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
}

export interface LocationReportData {
  /** The live MapTiler canvas element */
  mapCanvas: HTMLCanvasElement;
  lat: number;
  lng: number;
  /** Reverse-geocoded address data */
  geocoding?: GeocodingResult | null;
  /** Broadband infrastructure data from the point-broadband endpoint */
  broadbandData?: BroadbandInfoData | null;
  /** Search radius (metres) used for the broadband query */
  searchRadius?: number;
  /** Optional custom report title */
  reportTitle?: string;
  /** Optional analysis section title */
  analysisTitle?: string;
  /** Optional area bounds for area-based analysis */
  bounds?: AnalysisBounds | null;
  /** Properties from the clicked QoS / fiber-coverage feature (if any) */
  fiberCoverage?: FiberCoverageData | null;
}

// ─── Public helpers ────────────────────────────────────────────────────────────

/**
 * Parses a raw `Record<string, unknown>` coming from the QoS map-layer click
 * into a strongly-typed `FiberCoverageData` object.
 */
export function parseFiberFeatureProperties(props: Record<string, unknown>): FiberCoverageData {
  const raw = (key: string): string =>
    ((props[key] ?? props[key.charAt(0).toUpperCase() + key.slice(1)] ?? "") as string).trim();

  const rawProviders = raw("providers") || raw("provider");
  const providers = rawProviders
    .split(/[,\/]/)
    .map((p) => p.trim())
    .filter(Boolean);

  return {
    region: raw("region"),
    province: raw("province"),
    municipality: raw("municipality"),
    barangay: raw("barangay"),
    zipcode: raw("zipcode"),
    providers,
  };
}

/**
 * Generates a print-ready HTML location report, opens it in a new window,
 * and auto-triggers the browser's print dialog.
 */
export function generateLocationReport(data: LocationReportData): void {
  // Capture the map canvas as a data-URL *synchronously* (caller is responsible
  // for calling inside an idle event so the canvas has content).
  const mapImageDataUrl = data.mapCanvas.toDataURL("image/png");

  const now = new Date();
  const reportDate = now.toLocaleDateString("en-PH", {
    weekday: "long",
    year: "numeric",
    month: "long",
    day: "numeric",
  });
  const reportTime = now.toLocaleTimeString("en-PH", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });

  const html = buildReportHTML(data, mapImageDataUrl, reportDate, reportTime);

  const win = window.open("", "_blank");
  if (!win) {
    alert("Popup blocked. Please allow popups for this site to generate the report.");
    return;
  }

  try {
    // Detach the report window from the app tab so print interactions don't affect app navigation state.
    win.opener = null;
  } catch {
    // Ignore if browser prevents mutating opener.
  }

  win.document.write(html);
  win.document.close();
  win.focus();
  // Small delay so the browser renders before printing
  setTimeout(() => {
    try {
      win.print();
    } catch {
      // Ignore print failures and keep report tab open for manual printing.
    }
  }, 800);
}

// ─── Internal HTML builder ─────────────────────────────────────────────────────

function esc(value: unknown): string {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function buildReportHTML(
  data: LocationReportData,
  mapImageDataUrl: string,
  reportDate: string,
  reportTime: string,
): string {
  const {
    lat,
    lng,
    geocoding,
    broadbandData,
    searchRadius = 500,
    reportTitle = "Broadband Location Analysis Report",
    analysisTitle = "Location Analysis",
    bounds,
    fiberCoverage,
  } = data;

  const hasAddress =
    geocoding && Object.keys(geocoding).some((k) => k !== "country" && !!geocoding[k as keyof GeocodingResult]);

  const addressLine = [geocoding?.address, geocoding?.place, geocoding?.municipality]
    .filter(Boolean)
    .join(", ");
  const regionLine = [geocoding?.region, geocoding?.postal_code].filter(Boolean).join(" ");
  const headerCoordinateLine = bounds ? `Center ${lat.toFixed(6)}, ${lng.toFixed(6)}` : `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
  const boundsLine = bounds
    ? `SW ${bounds.minLat.toFixed(4)}, ${bounds.minLng.toFixed(4)} | NE ${bounds.maxLat.toFixed(4)}, ${bounds.maxLng.toFixed(4)}`
    : "";
  const analysisDetails = bounds
    ? `
      <div class="kv">
        <span class="k">Center Point</span>
        <span class="v" style="font-family:monospace">${lat.toFixed(6)}, ${lng.toFixed(6)}</span>
      </div>
      <div class="kv">
        <span class="k">Analysis Bounds</span>
        <span class="v">SW ${bounds.minLat.toFixed(4)}, ${bounds.minLng.toFixed(4)}<br />NE ${bounds.maxLat.toFixed(4)}, ${bounds.maxLng.toFixed(4)}</span>
      </div>
      <div class="kv">
        <span class="k">Envelope Radius</span>
        <span class="v">${searchRadius} m</span>
      </div>`
    : `
      <div class="kv">
        <span class="k">Coordinates</span>
        <span class="v" style="font-family:monospace">${lat.toFixed(6)}, ${lng.toFixed(6)}</span>
      </div>
      <div class="kv">
        <span class="k">Search Radius</span>
        <span class="v">${searchRadius} m</span>
      </div>`;

  // ── Service Provider rows ────────────────────────────────────────────────────
  const providerRows = (broadbandData?.serviceProviders ?? [])
    .map(
      (p: ServiceProviderInfo) => `
      <tr>
        <td>
          <span class="dot" style="background:${esc(p.color || "#6B7280")}"></span>
          ${esc(p.name)}
        </td>
        <td><span class="badge">${esc(p.serviceType)}</span></td>
        <td class="num">${p.cellCount}</td>
        <td class="num">${p.napCount}</td>
        <td class="num">${p.totalAssets}</td>
      </tr>`,
    )
    .join("");

  // ── Municipality rows ────────────────────────────────────────────────────────
  const munRows = (broadbandData?.municipalities ?? [])
    .map(
      (m: MunicipalityInfo) => `
      <tr>
        <td>${esc(m.displayName)}</td>
        <td>
          <span class="badge ${m.hasAssets ? "badge-green" : "badge-gray"}">
            ${esc(m.coverageStatus.replace(/_/g, " "))}
          </span>
        </td>
        <td class="num">${m.hasAssets ? m.cellCount : "—"}</td>
        <td class="num">${m.hasAssets ? m.napCount : "—"}</td>
        <td class="num">${m.hasAssets ? m.assetPercentage.toFixed(1) + "%" : "—"}</td>
      </tr>`,
    )
    .join("");

  // ── Fiber coverage detail rows ───────────────────────────────────────────────
  const fiberProviderBadges = (fiberCoverage?.providers ?? [])
    .map((p) => `<span class="provider-badge">${esc(p)}</span>`)
    .join(" ");

  const fiberSection = fiberCoverage
    ? `
    <div class="section page-break">
      <h2 class="section-title">
        <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M8.111 16.404a5.5 5.5 0 017.778 0M12 20h.01m-7.08-7.071c3.904-3.905 10.236-3.905 14.141 0M1.394 9.393c5.857-5.857 15.355-5.857 21.213 0"/>
        </svg>
        Fiber Coverage Details
      </h2>
      <div class="two-col">
        ${fiberCoverage.region ? `<div class="kv"><span class="k">Region</span><span class="v">${esc(fiberCoverage.region)}</span></div>` : ""}
        ${fiberCoverage.province ? `<div class="kv"><span class="k">Province</span><span class="v">${esc(fiberCoverage.province)}</span></div>` : ""}
        ${fiberCoverage.municipality ? `<div class="kv"><span class="k">Municipality</span><span class="v">${esc(fiberCoverage.municipality)}</span></div>` : ""}
        ${fiberCoverage.barangay ? `<div class="kv"><span class="k">Barangay</span><span class="v">${esc(fiberCoverage.barangay)}</span></div>` : ""}
        ${fiberCoverage.zipcode ? `<div class="kv"><span class="k">Zip Code</span><span class="v">${esc(fiberCoverage.zipcode)}</span></div>` : ""}
      </div>
      ${
        fiberCoverage.providers.length > 0
          ? `<div class="providers-section">
              <p class="providers-label">Internet Service Providers (${fiberCoverage.providers.length})</p>
              <div class="providers-list">${fiberProviderBadges}</div>
            </div>`
          : `<p class="muted">No provider information available for this coverage area.</p>`
      }
    </div>`
    : "";

  // ── Broadband infrastructure section ─────────────────────────────────────────
  const broadbandSection = broadbandData
    ? `
    <div class="section">
      <h2 class="section-title">
        <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M1.5 8.5C5.2 4.8 10.3 2.5 12 2.5S18.8 4.8 22.5 8.5"/>
          <path d="M4.5 11.5C7.1 8.9 9.4 7.5 12 7.5s4.9 1.4 7.5 4"/>
          <path d="M7.5 14.5C9.2 12.8 10.5 12 12 12s2.8.8 4.5 2.5"/>
          <circle cx="12" cy="17" r="1.5"/>
        </svg>
        Broadband Infrastructure Summary
      </h2>

      <!-- Stat cards -->
      <div class="stat-grid">
        <div class="stat-card stat-blue">
          <div class="stat-label">Cell Sites</div>
          <div class="stat-value">${broadbandData.summary.totalCells}</div>
        </div>
        <div class="stat-card stat-green">
          <div class="stat-label">NAPs</div>
          <div class="stat-value">${broadbandData.summary.totalNaps}</div>
        </div>
        <div class="stat-card stat-purple">
          <div class="stat-label">Providers</div>
          <div class="stat-value">${broadbandData.summary.serviceProviderCount}</div>
        </div>
        <div class="stat-card stat-orange">
          <div class="stat-label">Total Assets</div>
          <div class="stat-value">${broadbandData.summary.totalAssets}</div>
        </div>
      </div>

      <!-- Coverage area -->
      <div class="coverage-box">
        <h3 class="sub-title">Coverage Area</h3>
        <div class="four-col">
          <div class="kv">
            <span class="k">Area Size</span>
            <span class="v">${broadbandData.summary.coverageAreaKm2.toFixed(2)} km²</span>
          </div>
          <div class="kv">
            <span class="k">Asset Density</span>
            <span class="v">${broadbandData.summary.assetDensityPerKm2.toFixed(1)} /km²</span>
          </div>
          <div class="kv">
            <span class="k">Municipalities</span>
            <span class="v">${broadbandData.summary.municipalityCount}</span>
          </div>
          <div class="kv">
            <span class="k">With Assets</span>
            <span class="v">${broadbandData.summary.municipalitiesWithAssets}</span>
          </div>
        </div>
        <div class="radius-note">
          Search radius: ${searchRadius}m
        </div>
      </div>

      <!-- Service providers table -->
      ${
        providerRows
          ? `<h3 class="sub-title">Service Providers</h3>
             <table>
               <thead>
                 <tr>
                   <th>Provider</th>
                   <th>Type</th>
                   <th class="num">Cell Sites</th>
                   <th class="num">NAPs</th>
                   <th class="num">Total</th>
                 </tr>
               </thead>
               <tbody>${providerRows}</tbody>
             </table>`
          : `<p class="muted">No service provider data available.</p>`
      }
    </div>

    <!-- Municipalities -->
    ${
      munRows
        ? `<div class="section page-break">
             <h2 class="section-title">
               <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                 <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/>
                 <polyline points="9 22 9 12 15 12 15 22"/>
               </svg>
               Municipalities Coverage
             </h2>
             <table>
               <thead>
                 <tr>
                   <th>Municipality</th>
                   <th>Status</th>
                   <th class="num">Cell Sites</th>
                   <th class="num">NAPs</th>
                   <th class="num">Coverage</th>
                 </tr>
               </thead>
               <tbody>${munRows}</tbody>
             </table>
           </div>`
        : ""
    }`
    : "";

  // ── Full HTML document ────────────────────────────────────────────────────────
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>NTC ${esc(reportTitle)} — ${esc(reportDate)}</title>
  <style>
    /* ── Reset & base ──────────────────────────────────────────────── */
    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Segoe UI', Arial, sans-serif;
      font-size: 13px;
      color: #1a1a2e;
      background: #f8fafc;
      line-height: 1.5;
    }

    /* ── Page layout ───────────────────────────────────────────────── */
    .page { max-width: 900px; margin: 0 auto; padding: 32px 24px; }

    /* ── Header ────────────────────────────────────────────────────── */
    .report-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 16px;
      padding: 20px 24px;
      background: linear-gradient(135deg, #1e3a5f 0%, #2563eb 100%);
      border-radius: 12px;
      color: #fff;
      margin-bottom: 20px;
    }
    .ntc-logo { display: flex; align-items: center; gap: 12px; }
    .ntc-shield {
      width: 44px; height: 44px;
      background: rgba(255,255,255,0.15);
      border-radius: 10px;
      display: flex; align-items: center; justify-content: center;
      font-weight: 900; font-size: 15px; letter-spacing: 0.5px;
      border: 2px solid rgba(255,255,255,0.3);
    }
    .ntc-name { font-size: 11px; opacity: 0.85; letter-spacing: 0.5px; }
    .ntc-title { font-size: 16px; font-weight: 700; margin-top: 1px; }
    .report-meta { text-align: right; font-size: 11px; opacity: 0.85; }
    .report-meta .coords {
      font-size: 13px;
      font-weight: 600;
      opacity: 1;
      margin-bottom: 4px;
      font-family: 'Courier New', monospace;
    }
    .report-meta .bounds {
      max-width: 280px;
      line-height: 1.4;
    }

    /* ── Map image ──────────────────────────────────────────────────── */
    .map-container {
      border-radius: 12px;
      overflow: hidden;
      border: 1px solid #e2e8f0;
      margin-bottom: 20px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.07);
    }
    .map-container img { display: block; width: 100%; height: auto; }
    .map-caption {
      background: #f1f5f9;
      padding: 6px 12px;
      font-size: 11px;
      color: #64748b;
      border-top: 1px solid #e2e8f0;
    }

    /* ── Section ────────────────────────────────────────────────────── */
    .section {
      background: #fff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 20px;
      margin-bottom: 16px;
      box-shadow: 0 1px 4px rgba(0,0,0,0.04);
    }
    .section-title {
      font-size: 14px;
      font-weight: 700;
      color: #1e3a5f;
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 14px;
      padding-bottom: 10px;
      border-bottom: 2px solid #e2e8f0;
    }
    .icon { width: 16px; height: 16px; color: #2563eb; flex-shrink: 0; }
    .sub-title {
      font-size: 12px;
      font-weight: 700;
      color: #374151;
      margin: 14px 0 8px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    /* ── Key-value pairs ────────────────────────────────────────────── */
    .two-col  { display: grid; grid-template-columns: 1fr 1fr; gap: 8px 24px; }
    .four-col { display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; gap: 8px; }
    .kv { padding: 6px 0; }
    .k { display: block; font-size: 10px; text-transform: uppercase; letter-spacing: 0.4px; color: #94a3b8; margin-bottom: 1px; }
    .v { display: block; font-weight: 600; color: #1e293b; font-size: 13px; }

    /* ── Stat cards ─────────────────────────────────────────────────── */
    .stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 16px; }
    .stat-card {
      border-radius: 10px;
      padding: 12px;
      text-align: center;
      border: 1px solid transparent;
    }
    .stat-blue   { background: #eff6ff; border-color: #bfdbfe; }
    .stat-green  { background: #f0fdf4; border-color: #bbf7d0; }
    .stat-purple { background: #faf5ff; border-color: #e9d5ff; }
    .stat-orange { background: #fff7ed; border-color: #fed7aa; }
    .stat-label { font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px; color: #64748b; }
    .stat-value { font-size: 28px; font-weight: 800; color: #1e293b; margin-top: 2px; }

    /* ── Coverage box ───────────────────────────────────────────────── */
    .coverage-box {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 12px;
      margin-bottom: 14px;
    }
    .radius-note {
      margin-top: 8px;
      font-size: 11px;
      color: #3b82f6;
      font-weight: 500;
    }

    /* ── Table ──────────────────────────────────────────────────────── */
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 12px;
    }
    th, td {
      padding: 8px 10px;
      text-align: left;
      border-bottom: 1px solid #f1f5f9;
    }
    th {
      background: #f8fafc;
      font-weight: 700;
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.4px;
      color: #64748b;
    }
    tr:last-child td { border-bottom: none; }
    tr:hover td { background: #fafafa; }
    td.num, th.num { text-align: right; }

    /* ── Badges ─────────────────────────────────────────────────────── */
    .badge {
      display: inline-flex;
      padding: 2px 8px;
      border-radius: 20px;
      font-size: 10px;
      font-weight: 600;
      background: #dbeafe;
      color: #1d4ed8;
    }
    .badge-green { background: #dcfce7; color: #15803d; }
    .badge-gray  { background: #f1f5f9; color: #64748b; }
    .dot {
      display: inline-block;
      width: 8px; height: 8px;
      border-radius: 50%;
      margin-right: 6px;
      vertical-align: middle;
    }

    /* ── Providers list ─────────────────────────────────────────────── */
    .providers-section { margin-top: 12px; }
    .providers-label { font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.4px; color: #94a3b8; margin-bottom: 6px; }
    .providers-list { display: flex; flex-wrap: wrap; gap: 6px; }
    .provider-badge {
      display: inline-flex;
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
      background: #eff6ff;
      color: #1d4ed8;
      border: 1px solid #bfdbfe;
    }

    /* ── Misc ───────────────────────────────────────────────────────── */
    .muted { color: #94a3b8; font-style: italic; font-size: 12px; }
    .print-btn {
      display: block;
      width: 100%;
      padding: 12px;
      margin-bottom: 16px;
      background: #2563eb;
      color: #fff;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.15s;
    }
    .print-btn:hover { background: #1d4ed8; }

    /* ── Print styles ───────────────────────────────────────────────── */
    @media print {
      body { background: #fff; font-size: 11px; }
      .page { padding: 0; max-width: 100%; }
      .print-btn { display: none !important; }
      .stat-value { font-size: 22px; }
      .map-container img { max-height: 400px; object-fit: cover; }
      .page-break { page-break-before: always; }
      .section { box-shadow: none; border: 1px solid #d1d5db; break-inside: avoid; }
    }

    @page { margin: 18mm 15mm; }
  </style>
</head>
<body>
<div class="page">
  <!-- Print button (hidden when printing) -->
  <button class="print-btn" onclick="window.print()">🖨️ Print / Save as PDF</button>

  <!-- Header -->
  <div class="report-header">
    <div class="ntc-logo">
      <div class="ntc-shield">NTC</div>
      <div>
        <div class="ntc-name">National Telecommunications Commission</div>
        <div class="ntc-title">${esc(reportTitle)}</div>
      </div>
    </div>
    <div class="report-meta">
      <div class="coords">${headerCoordinateLine}</div>
      ${boundsLine ? `<div class="bounds">${esc(boundsLine)}</div>` : ""}
      <div>${esc(reportDate)}</div>
      <div>${esc(reportTime)}</div>
    </div>
  </div>

  <!-- Map Screenshot -->
  <div class="map-container">
    <img src="${mapImageDataUrl}" alt="Map screenshot" />
    <div class="map-caption">Map capture — ${esc(reportDate)} ${esc(reportTime)}</div>
  </div>

  <!-- Location Analysis -->
  <div class="section">
    <h2 class="section-title">
      <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M21 10c0 7-9 13-9 13S3 17 3 10a9 9 0 0118 0z"/>
        <circle cx="12" cy="10" r="3"/>
      </svg>
      ${esc(analysisTitle)}
    </h2>
    <div class="two-col">
      ${analysisDetails}
      ${
        hasAddress
          ? `
      <div class="kv">
        <span class="k">${bounds ? "Area Reference" : "Address"}</span>
        <span class="v">${esc(addressLine || "—")}</span>
      </div>
      <div class="kv">
        <span class="k">Region / Postal</span>
        <span class="v">${esc(regionLine || "—")}</span>
      </div>`
          : ""
      }
    </div>
  </div>

  <!-- Broadband section (cells/NAPs/providers) -->
  ${broadbandSection}

  <!-- Fiber coverage section -->
  ${fiberSection}

  <!-- Footer -->
  <div style="text-align:center; font-size:10px; color:#94a3b8; margin-top:24px; padding-top:12px; border-top:1px solid #e2e8f0;">
    Generated by NTC QoS &amp; Broadband Mapping Portal &bull; ${esc(reportDate)} ${esc(reportTime)}
  </div>
</div>
</body>
</html>`;
}
