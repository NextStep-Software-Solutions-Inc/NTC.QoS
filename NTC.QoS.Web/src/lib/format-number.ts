// e.g 100000 format as 100K or 100,000. swith type to format as currency
export function formatNumber(value: number, type: "currency" | "short" = "short"): string {
  if (type === "currency") {
    return new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" }).format(value);
  }

  if (value >= 1e6) {
    return `${(value / 1e6).toFixed(1)}M`;
  } else if (value >= 1e3) {
    return `${(value / 1e3).toFixed(1)}K`;
  } else {
    return value.toString();
  }
}
