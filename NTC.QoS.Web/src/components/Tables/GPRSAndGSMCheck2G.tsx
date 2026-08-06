import React from "react";

import { Checkbox } from "../ui/checkbox";
import { Label } from "../ui/label";

interface BandsGPRSAndGSMCheckProps {
  bands?: Record<string, boolean>;
  gprsChecked?: boolean;
  gsmChecked?: boolean;
  setGprsChecked?: (checked: boolean) => void;
  setGsmChecked?: (checked: boolean) => void;
  toggleBand?: (key: "gsm-850" | "gsm-900" | "gsm-1800" | "gsm-1900") => void;
}

const GPRSAndGSMCheck2G = ({
  bands,
  gprsChecked,
  gsmChecked,
  setGprsChecked,
  setGsmChecked,
  toggleBand,
}: BandsGPRSAndGSMCheckProps) => {
  return (
    <div>
      {/* GPRS */}

      <div className="mb-3 border rounded-md p-3">
        <div className="flex items-center space-x-2">
          <Checkbox id="gprs" checked={gprsChecked} onCheckedChange={(checked) => setGprsChecked?.(Boolean(checked))} />
          <Label htmlFor="gprs" className="text-sm font-medium">
            GPRS
          </Label>
        </div>

        {gprsChecked && (
          <div className="mt-3 ml-4 space-y-2">
            <Label className="text-sm font-medium">Bands</Label>
            <div className="grid grid-cols-2 gap-2">
              {Object.entries(bands ?? {}).map(([key, value]) => (
                <div key={key} className="flex items-center space-x-2">
                  <Checkbox id={key} checked={value} onCheckedChange={() => toggleBand?.(key as keyof typeof bands)} />
                  <Label htmlFor={key} className="text-sm">
                    {key.toUpperCase().replace("-", " ")}
                  </Label>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* GSM */}
      <div className="mb-6 border rounded-md px-3 py-2">
        <div className="flex items-center space-x-2">
          <Checkbox id="gsm" checked={gsmChecked} onCheckedChange={(checked) => setGsmChecked?.(Boolean(checked))} />
          <Label htmlFor="gsm" className="text-sm font-medium">
            GSM
          </Label>
        </div>
      </div>
    </div>
  );
};

export default GPRSAndGSMCheck2G;
