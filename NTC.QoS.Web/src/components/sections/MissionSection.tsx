import Image from "next/image";

import { Card, CardContent } from "@/components/ui/card";

export default function MissionSection() {
  return (
    <section className="py-16 px-4 bg-white">
      <div className="container mx-auto max-w-7xl">
        <div className="flex flex-col md:flex-row items-stretch gap-8 max-w-6xl mx-auto">
          <div className="w-full md:w-1/2">
            <Card className="border-gray-200 h-full overflow-hidden p-0">
              <div className="relative w-full h-full min-h-[300px]">
                <Image
                  src="/assets/images/Our-mission.jpg"
                  alt="NTC Mission Image"
                  fill
                  className="object-cover"
                  priority
                />
              </div>
            </Card>
          </div>
          <div className="w-full md:w-1/2">
            <Card className="border-gray-200 h-full">
              <CardContent className="p-6 md:p-8">
                <h2 className="text-2xl md:text-3xl font-bold mb-4 text-[#010B1C]">Our Mission</h2>
                <p className="text-[#010B1C] text-base md:text-lg">
                  {`Our mission is to empower Filipinos with transparent,
                  reliable, and accessible broadband information.
                  We believe that everyone deserves fast, reliable internet,
                  whether you're working from home, streaming, gaming, or
                  simply staying connected.`}
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </section>
  );
}
