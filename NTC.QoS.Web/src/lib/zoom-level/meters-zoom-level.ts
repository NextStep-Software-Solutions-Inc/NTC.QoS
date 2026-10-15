export function metersToPixelsAtMaxZoom(zoom: number) {
  const earthCircumference = 40075000; // Earth's circumference in meters
  const pixelsAtZoom0 = 256; // Pixels at zoom 0
  const metersPerPixel = earthCircumference / pixelsAtZoom0;
  return metersPerPixel / Math.pow(2, zoom);
}
