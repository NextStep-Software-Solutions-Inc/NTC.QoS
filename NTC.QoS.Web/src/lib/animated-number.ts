// Parses a number or short string (e.g., "1.8K", "43%", "2M") into [number, suffix].
// Returns [value, suffix] where suffix can be "K", "M", "G", "T", "%", or "".
export function parseShortNumber(val: number | string): [number, string] {
  if (typeof val === "number") {
    return [val, ""];
  }

  // Match: number (int or float), optional suffix (K, M, G, T, %)
  const regex = /^(\d*\.?\d+)\s*([KMGT%]?)$/i;
  const match = regex.exec(val.trim());
  if (!match) {
    throw new Error(`Invalid number format: ${val}`);
  }

  const value = parseFloat(match[1]!);
  const suffix = match[2]?.toUpperCase() ?? "";

  return [value, suffix];
}

// Animate number from 0 to end, calling callback with the current value (number).
// Accepts end as number or string like "1.8K".
// To display as "1.8K", use formatNumberShort in your render logic.
export function animateNumber(end: number, duration = 2000, callback: (value: number) => void): void {
  const start = 0;
  const startTime = performance.now();

  function animate(currentTime: number) {
    const elapsed = currentTime - startTime;
    const progress = Math.min(elapsed / duration, 1);
    const value = start + (end - start) * progress;

    callback(+value.toFixed(2)); // Format to 2 decimal places

    if (progress < 1) {
      requestAnimationFrame(animate);
    }
  }

  requestAnimationFrame(animate);
}
