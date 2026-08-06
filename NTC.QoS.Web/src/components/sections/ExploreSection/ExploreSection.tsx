import React from "react";
import Image from "next/image";

import ExploreSectionButton from "./ExploreSectionButton";

const ExploreSection = () => {
  return (
    <section className="flex items-center justify-center overflow-hidden lg:p-12 ">
      <div className="container mx-auto gap-10 lg:gap-0 max-w-7xl flex w-full flex-wrap justify-between px-7">
        {/* <div className="w-full border sm:w-1/2">a</div>
        <div className="w-full border sm:w-1/2">b</div> */}
        <div className="flex w-full flex-col justify-center sm:w-1/2">
          {/* <div className="flex w-full grow flex-col justify-center p-4 lg:p-16"> */}
          <h1 className="text-6xl font-bold leading-tight text-gray-900 md:text-5xl">
            Start Exploring
            <br />
            Now
          </h1>
          <p className="mb-6 mt-2 text-lg text-gray-600">Instantly check providers & speeds near you!</p>
          <ExploreSectionButton />
        </div>

        <div className="flex w-full flex-col justify-center sm:w-1/2">
          {/* <div className="mt-6 h-[400px] flex-1 grow lg:mt-0 lg:h-[500px] lg:p-8"> */}
          <Image
            src="/assets/images/coverage-map.png"
            alt="Map"
            width={500}
            height={400}
            // className="h-full w-full bg-blue-400 object-cover"
            className="bg-blue-400 object-cover"
          />
        </div>
      </div>
    </section>
  );
};

export default ExploreSection;
