"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";

const networkData = [
  { name: "Bands", type: "GSM 2G", icon: "CheckSquare", bands: ["BAND 3", "BAND 5"] },
  { name: "Bands", type: "3G WCDMA", icon: "Signal", bands: ["BAND 3", "BAND 5"] },
  { name: "Frequencies", type: "4G TDD LTE", icon: "Signal", frequencies: ["2100", "900", "2600", "3500"] },
  { name: "Frequencies", type: "4G FDD LTE", icon: "Signal", frequencies: ["2100", "900", "2600", "3500"] },
];

const NetworkBands = () => {
  return (
    <div className="space-y-2 mt-3">
      {networkData.map((item, index) => {
        // const LucideIcon = Icons[item.icon as keyof typeof Icons];
        return (
          <Card key={index} className="w-full h-fit">
            <CardHeader className="flex items-center gap-2">
              <CardTitle className="text-base">{item.type}</CardTitle>
            </CardHeader>
            <CardContent>
              <div>{item.name}</div>

              {item.bands?.map((band, idx) => (
                <div key={idx} className="flex items-center gap-2">
                  <Checkbox id={`${item.type}-${band}`} checked />
                  <label htmlFor={`${item.type}-${band}`} className="text-sm">
                    {band}
                  </label>
                </div>
              ))}

              {item.frequencies?.map((freq, idx) => (
                <div key={idx} className="flex items-center gap-2">
                  <Checkbox id={`${item.type}-${freq}`} checked />
                  <label htmlFor={`${item.type}-${freq}`} className="text-sm">
                    {freq}
                  </label>
                </div>
              ))}
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
};

export default NetworkBands;
