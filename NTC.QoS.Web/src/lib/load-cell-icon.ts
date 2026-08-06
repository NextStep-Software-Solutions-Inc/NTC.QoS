import { type Map } from "@maptiler/sdk";

export const loadCellIcon = async (map: Map) => {
  const { data } = await map.loadImage("/assets/images/cellsite-blue.png");
  map.addImage("cellular-icon", data);

  return map;
};
