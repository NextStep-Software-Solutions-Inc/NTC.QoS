import Image from "next/image";
import * as maptilersdk from "@maptiler/sdk";

import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";

const mapTypes = [
  {
    src: "https://cloud.maptiler.com/static/img/maps/hybrid.png",
    alt: "hybrid map",
    width: 40,
    height: 40,
    mapStyle: maptilersdk.MapStyle.HYBRID,
  },
  {
    src: "https://cloud.maptiler.com/static/img/maps/topo.png",
    alt: "topographic map",
    width: 40,
    height: 40,
    mapStyle: maptilersdk.MapStyle.TOPO,
  },
  {
    src: "https://cloud.maptiler.com/static/img/maps/topo-v4-dark.png",
    alt: "topographic dark map",
    width: 40,
    height: 40,
    mapStyle: maptilersdk.MapStyle.TOPO.DARK,
  },
  {
    src: "https://cloud.maptiler.com/static/img/maps/streets-v4.png",
    alt: "street map",
    width: 40,
    height: 40,
    mapStyle: maptilersdk.MapStyle.STREETS_V4,
  },
  {
    src: "https://cloud.maptiler.com/static/img/maps/streets-v4-dark.png",
    alt: "night map",
    width: 40,
    height: 40,
    mapStyle: maptilersdk.MapStyle.STREETS_V4.DARK,
  },
];

const MapType = () => {
  const { setMapStyle } = useMap();

  return (
    <div className={cn(" bg-gray-600/5 py-2 px-4 rounded-lg shadow-xl md:gap-x-6")}>
      <div className="text-sm font-semibold text-gray-600 mb-2">Map Type</div>
      <div className="flex flex-row items-center justify-between gap-x-4">
        {mapTypes.map((mapType) => (
          <div
            key={mapType.mapStyle.getName()}
            className="flex flex-col items-center justify-center"
            onClick={() => setMapStyle(mapType.mapStyle)}>
            <div
              className={cn(
                "bg-white rounded-md p-1 overflow-hidden transition-transform duration-300 hover:scale-110",
              )}>
              <Image
                width={mapType.width}
                height={mapType.height}
                alt={mapType.alt}
                src={mapType.src}
                unoptimized
                className={cn("object-cover rounded-md w-10 h-10")}
              />
            </div>
            <span className="text-xs font-medium mt-1 text-gray-600">{mapType.mapStyle.getName()}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default MapType;
