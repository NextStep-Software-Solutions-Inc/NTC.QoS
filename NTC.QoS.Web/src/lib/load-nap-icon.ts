import { type Map } from "@maptiler/sdk";

import { type Provider } from "@/components/features/provider/types/Provider";

export const createPulsingDot = (map: Map, providers: Provider[]) => {
  // map.addImage("pulsing-dot", data);
  const size = 100;

  const create = (color = "#0100ff80") => {
    const pulsingDot = {
      width: size,
      height: size,
      context: null as unknown as CanvasRenderingContext2D | null,
      data: new Uint8ClampedArray(size * size * 4),
      fillStyle: color,

      // get rendering context for the map canvas when layer is added to the map
      onAdd: function () {
        const canvas = document.createElement("canvas");
        canvas.width = this.width;
        canvas.height = this.height;
        this.context = canvas.getContext("2d");
      },

      // called once before every frame where the icon will be used
      render: function () {
        const duration = 1000;
        const t = (performance.now() % duration) / duration;

        const radius = (size / 2) * 0.3;
        const outerRadius = (size / 2) * 0.7 * t + radius;
        const context = this.context;

        // draw outer circle
        if (!context) return;
        context.clearRect(0, 0, this.width, this.height);
        context.beginPath();
        context.arc(this.width / 2, this.height / 2, outerRadius, 0, Math.PI * 2);
        context.fillStyle = "rgba(0, 50, 150," + (1 - t) + ")";
        context.fill();
        context.fillStyle = this.fillStyle;
        // draw inner circle
        context.beginPath();
        context.arc(this.width / 2, this.height / 2, radius, 0, Math.PI * 2);
        // context.fillStyle = NAPsProviderColor[0] ?? ""  // Default to black if array is empty
        context.strokeStyle = "white";
        context.lineWidth = 2 + 4 * (1 - t);
        context.fill();
        context.stroke();

        // update this image's data with data from the canvas
        this.data = context.getImageData(0, 0, this.width, this.height).data;

        // continuously repaint the map, resulting in the smooth animation of the dot
        map.triggerRepaint();

        // return `true` to let the map know that the image was updated
        return true;
      },
    };
    return pulsingDot;
  };

  // Add custom pulsing dots
  map.addImage("pulsing-dot", create(), { pixelRatio: 2 });
  providers.forEach((provider) => {
    const providerColor = provider.color;

    const providerName = provider.name.toLowerCase();
    map.addImage(`pulsing-dot-${providerName.toLocaleLowerCase()}`, create(providerColor.toLocaleLowerCase()), {
      pixelRatio: 2,
    });
  });
  return map;
};
