"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ArrowRight, Play } from "lucide-react";

import NetworkIllustration from "../illustrations/NetworkIllustration";

export default function HeroSectionV2() {
  const [isVisible, setIsVisible] = useState(false);
  const [mapRevealed, setMapRevealed] = useState(false);

  useEffect(() => {
    // Use setTimeout to avoid synchronous setState in effect
    const visibilityTimer = setTimeout(() => setIsVisible(true), 0);
    const mapTimer = setTimeout(() => setMapRevealed(true), 500);

    return () => {
      clearTimeout(visibilityTimer);
      clearTimeout(mapTimer);
    };
  }, []);

  return (
    <section className="relative min-h-screen flex items-center justify-center overflow-hidden bg-gradient-to-br from-blue-50 via-white to-yellow-50">
      {/* Level 0: Static PH Map Background - revealed on right side */}
      <div className="absolute inset-0 overflow-hidden">
        <div
          className="absolute inset-0 bg-repeat "
          style={{
            backgroundImage: `url('/assets/images/phil-static-map.webp')`,
            backgroundSize: "100%",
            backgroundPosition: "50% center",
            opacity: 0.6,
          }}
        />
        {/* Cloud-like mask to reveal map on right side */}
        <div
          className="absolute inset-0"
          style={{
            background:
              "radial-gradient(ellipse at 100% 50%, rgba(255,255,255,1) 40%, rgba(255,255,255,0.7) 60%, rgba(255,255,255,0.3) 80%, rgba(255,255,255,0) 100%)",
            opacity: mapRevealed ? 0 : 1,
            transition: "opacity 2s ease-in-out",
          }}
        />
        {/* Gradient mask to reveal map on right side and hide on left */}
        <div
          className="absolute inset-0"
          style={{
            background:
              "radial-gradient(ellipse at 10% 50%, rgba(255,255,255,1) 40%, rgba(255,255,255,0.7) 60%, rgba(255,255,255,0.3) 80%, rgba(255,255,255,0) 100%)",
            opacity: mapRevealed ? 1 : 0,
            transition: "opacity 2s ease-in-out",
          }}
        />
      </div>
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-red-400/20 rounded-full blur-3xl animate-blob" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-yellow-400/20 rounded-full blur-3xl animate-blob animation-delay-2000" />
        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-blue-300/10 rounded-full blur-3xl animate-blob animation-delay-4000" />
      </div>
      {/* Network illustration background */}
      <div className="absolute inset-0 opacity-30">
        <NetworkIllustration />
      </div>
      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="grid lg:grid-cols-2 gap-12 lg:gap-16 items-center">
          {/* Content */}
          <div
            className={`space-y-8 transform transition-all duration-1000 ${
              isVisible ? "translate-y-0 opacity-100" : "translate-y-10 opacity-0"
            }`}>
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-red-100 text-red-700 rounded-full text-sm font-medium">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75" />
                <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500" />
              </span>
              Official NTC Platform
            </div>

            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold text-gray-900 leading-tight">
              Discover{" "}
              <span className="bg-gradient-to-r from-red-600 to-yellow-600 bg-clip-text text-transparent">
                Broadband Coverage
              </span>{" "}
              Across the Philippines
            </h1>

            <p className="text-lg sm:text-xl text-gray-600 leading-relaxed">
              Access real-time internet coverage data, speed test results, and service quality metrics. Empowering
              Filipinos with transparent telecommunications insights.
            </p>

            <div className="flex flex-col sm:flex-row gap-4">
              <Link
                href="/public/map"
                className="group inline-flex items-center justify-center px-8 py-4 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl transform hover:-translate-y-0.5 transition-all duration-200">
                Explore Coverage Map
                <ArrowRight className="ml-2 w-5 h-5 group-hover:translate-x-1 transition-transform" />
              </Link>

              <Link
                href="https://drive.google.com/file/d/1x2crbHCUUrpL3YVogn8ZOfiJpPYht0BI/view" // Replace with actual demo video link
                target="_blank"
                rel="noopener noreferrer"
                className="group inline-flex items-center justify-center px-8 py-4 bg-white text-gray-700 rounded-xl font-semibold border-2 border-gray-200 hover:border-red-300 hover:bg-red-50 transition-all duration-200">
                <Play className="mr-2 w-5 h-5 group-hover:scale-110 transition-transform" />
                Watch Demo
              </Link>
            </div>
          </div>
        </div>
      </div>{" "}
      {/* Scroll indicator */}
      <div className="absolute bottom-8 left-1/2 transform -translate-x-1/2 animate-bounce">
        <div className="w-6 h-10 border-2 border-gray-400 rounded-full flex items-start justify-center p-2">
          <div className="w-1 h-3 bg-gray-400 rounded-full animate-scroll" />
        </div>
      </div>
    </section>
  );
}
