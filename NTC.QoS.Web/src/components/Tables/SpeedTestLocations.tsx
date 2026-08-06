import React from "react";
import Image from "next/image";
import { Globe, Pin } from "lucide-react";

import { Button } from "../ui/button";
import { Card, CardContent } from "../ui/card";

const providers = [
  {
    providerName: "Globe Telecom, Inc.",
    download: "122.72",
    upload: "71.79",
    ping: "57.51",
    signal: "-75 dBm",
    technologies: ["LTE", "HSPAP"],
    color: "bg-blue-600",
  },
  {
    providerName: "Smart Communications, Inc.",
    download: "100.00",
    upload: "50.00",
    ping: "30.00",
    signal: "-80 dBm",
    technologies: ["LTE", "HSPAP"],
    color: "bg-green-600",
  },
  {
    providerName: "Dito Telecommunity",
    download: "150.00",
    upload: "100.00",
    ping: "20.00",
    signal: "-70 dBm",
    technologies: ["LTE", "HSPAP"],
    color: "bg-red-600",
  },
  {
    providerName: "Converge ICT Solutions Inc.",
    download: "200.00",
    upload: "150.00",
    ping: "10.00",
    signal: "-60 dBm",
    technologies: ["Fiber", "HSPAP"],
    color: "bg-purple-600",
  },
];

const SpeedTestLocations = () => {
  return (
    <section className=" w-full mt-5">
      <div className="flex items-end gap-2 my-4">
        <Pin className="w-8 h-8 " />
        <h2 className="text-lg font-semibold"> Test Locations</h2>
      </div>
      <div className="w-full mb-5">
        <Image
          src={"/assets/images/fake-map.png"}
          alt="Speed Test Locations"
          width={800}
          height={500}
          className="rounded-md w-full h-64 object-center object-cover shadow-md"
        />
      </div>

      <div className="w-full mt-[2rem]">
        {providers.map((provider) => (
          <Card key={provider.providerName} className="w-full p-3 my-4">
            <div className="w-full  flex  justify-between my-3">
              <div className=" flex items-center gap-4">
                <div className="w-10 h-10 rounded-full bg-muted flex items-center justify-center">
                  <p className="text-2xl font-semibold text-muted-foreground">{provider.providerName.charAt(0)}</p>
                </div>
                <div className="text-left space-y-1">
                  <span className={`${provider.color} text-sm text-white py-1 px-3 rounded-full`}>
                    {provider.providerName}
                  </span>
                  <p className="text-sm text-muted-foreground">Service Provider</p>
                </div>
              </div>
              <div className="text-xs flex items-center space-x-2 bg-slate-100 py-1 px-5 rounded-full text-muted-foreground">
                <Globe className="w-4 h-4" />
                <p className="text-xs">
                  Location <strong className="text-foreground">1</strong>
                </p>
              </div>
            </div>

            <CardContent className="w-full space-y-4">
              {/* Metrics */}
              <div className="w-full grid grid-cols-1 sm:grid-cols-2 gap-2">
                <MetricBox label="Download" value={provider.download} unit="Mbps" />
                <MetricBox label="Upload" value={provider.upload} unit="Mbps" />
              </div>

              {/* Bottom Info */}
              <div className="space-y-3 mt-2 flex items-center justify-between">
                <section className="flex items-center gap-2">
                  <p className="text-md font-semibold">Coordinates</p>
                  <p className="text-muted-foreground text-md">1.3521, 103.8198</p>
                </section>
                <section className="flex items-center gap-2">
                  <p className="text-md font-semibold">Timestamp</p>
                  <p className="text-muted-foreground text-md">2025-10-01 12:00:00</p>
                </section>
              </div>
            </CardContent>
            <Button className="w-full mb-1" variant="outline">
              See Location
            </Button>
          </Card>
        ))}
      </div>
    </section>
  );
};

function MetricBox({ label, value, unit }: { label: string; value: string; unit: string }) {
  return (
    <div className="w-full border rounded-lg p-3">
      <p className="text-sm text-muted-foreground">{label}</p>
      <p className="text-xl font-semibold">
        {value} <span className="text-sm text-muted-foreground">{unit}</span>
      </p>
    </div>
  );
}

export default SpeedTestLocations;
