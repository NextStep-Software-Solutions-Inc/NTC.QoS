// data.ts

type ProvinceData = { id: number; province: string; municipality: number; barangay: number; isp: number | string };

export const mockData: ProvinceData[] = [
  { id: 1, province: "Bukidnon", municipality: 20, barangay: 464, isp: 3 },
  { id: 2, province: "Camiguin", municipality: 5, barangay: 464, isp: 3 },
  { id: 3, province: "Lanao Del Norte", municipality: 22, barangay: 464, isp: 3 },
  { id: 4, province: "Misamis Occidental", municipality: 14, barangay: 464, isp: 3 },
  { id: 5, province: "Misamis Oriental", municipality: 22, barangay: 464, isp: "Not yet listed" },
];
