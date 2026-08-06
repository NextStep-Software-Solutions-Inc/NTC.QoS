import { getSignalColor } from "./signal-level";

export const createMarker = (signalStrength: number) => {
  const element = document.createElement("div");
  element.style.width = "8px";
  element.style.height = "8px";
  element.style.borderRadius = "50%";
  element.style.backgroundColor = getSignalColor(signalStrength);
  element.style.border = "1px solid white";
  element.style.boxShadow = "0 0 2px rgba(0,0,0,0.3)";
  return element;
};
