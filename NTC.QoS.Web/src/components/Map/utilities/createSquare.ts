export default function createSquare(center: [number, number], radius: number) {
  const [lng, lat] = center;

  const widthScale = 1.011; // Scale factor to reduce width
  const lngRadius = radius * widthScale; // Adjust longitude radius for width scaling

  return [
    [lng - lngRadius, lat - radius], // Bottom-left
    [lng + lngRadius, lat - radius], // Bottom-right
    [lng + lngRadius, lat + radius], // Top-right
    [lng - lngRadius, lat + radius], // Top-left
    [lng - lngRadius, lat - radius], // Close the polygon
  ];
}
