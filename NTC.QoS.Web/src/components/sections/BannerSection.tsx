import React from "react";

const BannerSection = () => {
  return (
    <section
      className="h-[350px] flex items-center justify-center overflow-hidden py-5 text-white lg:p-12"
      style={{ background: "#DE0E23" }}>
      <div className="container mx-auto max-w-7xl flex w-full flex-wrap justify-between px-7 lg:gap-0">
        <div className="flex w-full flex-col justify-center sm:w-1/2">
          <h2 className="text-3xl font-bold md:text-4xl">
            Making Broadband <br />
            Choices Simple
          </h2>
        </div>
        <div className="flex w-full flex-col justify-center p-6 sm:w-1/2">
          <p className="text-base text-gray-100 md:text-lg">
            {`QOS, built and supported by NTC, helps you find the best 
              broadband provider in your area. Whether you're moving, renting, 
              or just looking for a better provider, our interactive Broadband 
              Map gives you the insights you need.`}
          </p>
        </div>
      </div>
    </section>
  );
};

export default BannerSection;
