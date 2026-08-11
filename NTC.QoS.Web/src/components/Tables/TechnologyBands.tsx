"use client";

import { CheckCircle, Signal, XCircle } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

// networkData.ts
const networkData = [
  {
    type: "GSM 2G",
    frequencies: [
      { value: "900 Mhz", supported: true },
      { value: "1800 Mhz", supported: true },
    ],
  },
  {
    type: "3G WCDMA",
    frequencies: [
      { value: "850 Mhz", supported: false },
      { value: "2100 mhz", supported: true },
    ],
  },
  {
    type: "4G TDD LTE",
    frequencies: [
      { value: "850 Mhz", supported: false },
      { value: "2100 mhz", supported: true },
      { value: "850 Mhz", supported: false },
      { value: "2100 mhz", supported: true },
    ],
  },
  {
    type: "4G FDD LTE",
    frequencies: [
      { value: "2300 Mhz", supported: false },
      { value: "2500 mhz", supported: false },
      { value: "2600 mhz", supported: true },
    ],
  },
  {
    type: "5G",
    frequencies: [
      { value: "2600 Mhz", supported: false },
      { value: "3500 Mhz", supported: true },
    ],
  },
];

const TechnologyBands = () => {
  return (
    <Card className="w-full border mt-3 p-3">
      <CardHeader className="">
        <CardTitle className="text-lg font-semibold flex items-center gap-x-2">
          <Signal className="h-5 w-5" />
          <p className="text-[20px]">Technologies</p>
        </CardTitle>
      </CardHeader>
      <div className="grid  grid-cols-1 sm:grid-cols-2 gap-4">
        {networkData.map((item, idx) => (
          <Card key={idx}>
            <CardHeader>
              <CardTitle className="text-base">{item.type}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {item.frequencies.map((freq, fIdx) => (
                <div
                  key={fIdx}
                  className={`flex items-center justify-between border px-3 py-1 rounded-md text-sm ${
                    freq.supported ? "border-green-400" : "border-red-400"
                  }`}>
                  <span>{freq.value}</span>
                  {freq.supported ? (
                    <CheckCircle className="h-4 w-4 text-green-500" />
                  ) : (
                    <XCircle className="h-4 w-4 text-red-500" />
                  )}
                </div>
              ))}
            </CardContent>
          </Card>
        ))}
      </div>
    </Card>
  );
};

export default TechnologyBands;
