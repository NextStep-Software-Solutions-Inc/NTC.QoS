export default function calculateRadius(
  cellSignal: { lat: number; lng: number; strength: number; barangay: string }[],
) {
  let minDistance = Infinity;

  for (let i = 0; i < cellSignal.length; i++) {
    for (let j = i + 1; j < cellSignal.length; j++) {
      const distance = Math.sqrt(
        Math.pow(cellSignal[i]!.lng - cellSignal[j]!.lng, 2) + Math.pow(cellSignal[i]!.lat - cellSignal[j]!.lat, 2),
      );
      if (distance < minDistance) {
        minDistance = distance;
      }
    }
  }

  // Set radius to half of the minimum distance to avoid overlap
  return minDistance / 2;
}
