import React from "react";
import { CheckIcon, SignalHigh, XIcon } from "lucide-react";

const TechGroup = ({
  title,
  frequencies,
}: {
  title: string;
  frequencies: { name: string | null; available: boolean }[];
}) => (
  <div className="p-4 border border-gray-200 rounded-lg h-full">
    <h3 className="text-sm font-semibold text-gray-500 mb-3">{title}</h3>
    <div className="grid grid-cols-2 gap-2">
      {frequencies.map((freq, index) => (
        <div key={index}>
          {freq.name ? (
            <div className="flex items-center justify-between px-3 py-1.5 bg-gray-50 rounded-md text-sm h-full">
              <span className="font-medium text-gray-700">{freq.name}</span>
              {freq.available ? (
                <CheckIcon className="w-5 h-5 text-green-500" />
              ) : (
                <XIcon className="w-5 h-5 text-red-500" />
              )}
            </div>
          ) : (
            <div />
          )}
        </div>
      ))}
    </div>
  </div>
);

const TechnologiesTab = () => {
  const technologies = {
    gsm: [
      { name: "900 Mhz", available: true },
      { name: "1800 Mhz", available: true },
    ],
    wcdma: [
      { name: "850 Mhz", available: false },
      { name: "2100 Mhz", available: true },
    ],
    tddLte: [
      { name: "850 Mhz", available: false },
      { name: "2100 Mhz", available: true },
      { name: "850 Mhz", available: false },
      { name: "2100 Mhz", available: true },
    ],
    fddLte: [
      { name: "2300 Mhz", available: false },
      { name: "2500 Mhz", available: true },
      { name: null, available: false },
      { name: "2600 Mhz", available: true },
    ],
  };

  return (
    <div className="bg-white p-4 rounded-lg border border-gray-200 h-full flex flex-col">
      <div className="flex items-center mb-4">
        <h2 className="text-xl font-bold text-gray-800 flex items-center">Overview of Available Technologies</h2>
        <SignalHigh className="w-6 h-6 text-gray-500 ml-2" />
      </div>

      {/* content grid should expand inside */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 flex-1">
        <TechGroup title="GSM 2G" frequencies={technologies.gsm} />
        <TechGroup title="3G WCDMA" frequencies={technologies.wcdma} />
        <TechGroup title="4G TDD LTE" frequencies={technologies.tddLte} />
        <TechGroup title="4G FDD LTE" frequencies={technologies.fddLte} />
      </div>
    </div>
  );
};

export default TechnologiesTab;
