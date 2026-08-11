export const getSignalColor = (signalStrength: number): string => {
  if (signalStrength >= -70) return "#00ff00"; // Strong signal: green
  if (signalStrength >= -85) return "#ffff00"; // Medium signal: yellow
  if (signalStrength >= -100) return "#ff9900"; // Weak signal: orange
  return "#ff0000"; // Very weak signal: red
};
