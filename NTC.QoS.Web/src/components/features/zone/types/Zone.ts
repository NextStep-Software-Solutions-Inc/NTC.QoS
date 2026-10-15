export type Zone = {
  id: string;
  name: string;
  description?: string;
  coordinates: { latitude: number; longitude: number }[];
  regionId: string;
  createdAt: string;
  updatedAt: string;
};

export type ZoneDetails = Zone;
