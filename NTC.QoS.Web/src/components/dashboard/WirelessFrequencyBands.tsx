import React from "react";

type ProviderKey = keyof typeof frequencyBands;
const WirelessFrequencyBands = ({ provider }: { provider: ProviderKey }) => {
  return (
    <div className="mt-5 space-y-6">
      <h4
        className={`text-sm font-bold ${provider === "globe" ? "text-blue-600" : provider === "smart" ? "text-green-600" : "text-red-600"}`}>
        {provider.toUpperCase()}
      </h4>

      {Object.entries(frequencyBands[provider]).map(([category, bands]) => (
        <div key={category} className="my-[5rem] flex items-center justify-center gap-5">
          <section className="w-[15%]">
            <h5
              className={`text-xs font-semibold ${provider === "globe" ? "text-blue-500" : provider === "smart" ? "text-green-500" : "text-red-500"}`}>
              {category.replace("_", " ")}
            </h5>
          </section>

          <div className="space-y-3">
            {(bands as { label: string; color: string }[]).map((band) => {
              return (
                <section
                  key={band.label}
                  className={`flex cursor-pointer items-center gap-1 space-x-2 rounded-full border px-3 py-1 hover:bg-gray-100`}>
                  <div
                    className={`flex items-center justify-center rounded-full px-3 py-3 text-white ${band.color}`}></div>
                  <p className="text-[12px] text-gray-500">{band.label}</p>
                </section>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
};

const frequencyBands = {
  globe: {
    GSM_2G: [
      { label: "Band 8 (900 MHz)", color: "bg-blue-200" },
      { label: "Band 3 (1800 MHz)", color: "bg-blue-300" },
    ],
    LTE_4G_FDD: [
      { label: "Band 28 (700 MHz)", color: "bg-blue-400" },
      { label: "Band 3 (1800 MHz)", color: "bg-blue-500" },
      { label: "Band 1 (2100 MHz)", color: "bg-blue-600" },
    ],
    LTE_4G_TDD: [
      { label: "Band 40 (2300 MHz)", color: "bg-blue-700" },
      { label: "Band 41 (2500 MHz)", color: "bg-blue-800" },
    ],
    NR_5G: [
      { label: "Band n78 (3500 MHz)", color: "bg-blue-900" },
      { label: "Band n41 (2500 MHz)", color: "bg-blue-950" },
    ],
  },
  smart: {
    GSM_2G: [
      { label: "Band 8 (900 MHz)", color: "bg-green-200" },
      { label: "Band 3 (1800 MHz)", color: "bg-green-300" },
    ],
    UMTS_3G: [
      { label: "Band 5 (850 MHz)", color: "bg-green-400" },
      { label: "Band 1 (2100 MHz)", color: "bg-green-500" },
    ],
    LTE_4G_FDD: [
      { label: "Band 28 (700 MHz)", color: "bg-green-600" },
      { label: "Band 3 (1800 MHz)", color: "bg-green-700" },
      { label: "Band 1 (2100 MHz)", color: "bg-green-800" },
    ],
    LTE_4G_TDD: [
      { label: "Band 40 (2300 MHz)", color: "bg-green-900" },
      { label: "Band 41 (2500 MHz)", color: "bg-green-950" },
    ],
    NR_5G: [
      { label: "Band n78 (3500 MHz)", color: "bg-green-950" },
      { label: "Band n41 (2500 MHz)", color: "bg-green-900" },
    ],
  },
  dito: {
    LTE_4G_FDD: [
      { label: "Band 28 (700 MHz)", color: "bg-red-500" },
      { label: "Band 3 (1800 MHz)", color: "bg-red-600" },
      { label: "Band 1 (2100 MHz)", color: "bg-red-700" },
    ],
    LTE_4G_TDD: [
      { label: "Band 40 (2300 MHz)", color: "bg-red-800" },
      { label: "Band 41 (2500 MHz)", color: "bg-red-900" },
    ],
    NR_5G: [{ label: "Band n78 (3500 MHz)", color: "bg-red-950" }],
  },
};

export default WirelessFrequencyBands;
