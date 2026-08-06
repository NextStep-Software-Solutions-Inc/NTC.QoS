import React from "react";
import Image from "next/image";
import { Check } from "lucide-react";

const BroadbandDetails = () => {
  const features = [
    "Check broadband availability before you buy, rent, or build.",
    "Compare providers and speeds to find the best option for your needs",
    "Learn about Ultra-Fast Broadband (UFB) and network coverage in your neighborhood.",
  ];

  return (
    <section className="flex items-center justify-center overflow-hidden lg:p-12">
      <div className="container mx-auto  max-w-7xl flex w-full flex-wrap justify-between px-7">
        <div className="flex w-full flex-col justify-center sm:w-1/2">
          <h2 className="mb-6 text-3xl font-bold leading-tight text-[#0A0F2C] md:text-[2.5rem]">
            Find the Best
            <br className="hidden md:block" />
            Broadband for You
          </h2>
          <p className="mb-8 text-base text-gray-600 md:text-lg">
            {`Whether you're moving, renting, or just looking for a better provider, 
              our interactive Broadband Map gives you the insights you need.`}
          </p>
          <div className="space-y-6 md:space-y-7">
            {features.map((feature, index) => (
              <div key={index} className="flex items-start gap-3 md:gap-4">
                <div className="mt-1 flex-shrink-0">
                  <div className="flex h-5 w-5 items-center justify-center rounded-full bg-[#3B82F6]">
                    <Check className="h-3.5 w-3.5 stroke-2 text-white" />
                  </div>
                </div>
                <p className="text-base leading-normal text-[#1E293B] md:text-lg">{feature}</p>
              </div>
            ))}
          </div>
        </div>
        <div className="flex w-full flex-col justify-center p-6 sm:w-1/2">
          <Image
            src="/assets/images/Map.png"
            alt="Interactive Broadband Map Preview"
            width={600}
            height={400}
            className="h-full w-full object-cover"
          />
        </div>
      </div>
    </section>
  );
};

export default BroadbandDetails;
