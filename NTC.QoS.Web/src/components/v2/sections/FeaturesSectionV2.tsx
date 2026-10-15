"use client";

import { useEffect, useRef, useState } from "react";
import { BarChart3, MapPin, Radio, Shield } from "lucide-react";

const features = [
  {
    icon: MapPin,
    title: "Interactive Coverage Map",
    description:
      "Explore detailed broadband coverage across all Philippine regions with real-time data visualization and filtering capabilities.",
    color: "blue",
  },
  {
    icon: BarChart3,
    title: "Speed Test Analytics",
    description:
      "Access comprehensive speed test results and performance metrics from millions of tests conducted nationwide.",
    color: "purple",
  },
  {
    icon: Radio,
    title: "Cell Site Database",
    description:
      "Browse our extensive database of cell towers and infrastructure information for transparency and planning.",
    color: "blue",
  },
  {
    icon: Shield,
    title: "Quality of Service Metrics",
    description: "Monitor service quality standards and ISP performance to ensure compliance with NTC regulations.",
    color: "purple",
  },
];

export default function FeaturesSectionV2() {
  const [visibleCards, setVisibleCards] = useState<number[]>([]);
  const sectionRef = useRef<HTMLElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            features.forEach((_, index) => {
              setTimeout(() => {
                setVisibleCards((prev) => [...prev, index]);
              }, index * 150);
            });
          }
        });
      },
      { threshold: 0.1 },
    );

    if (sectionRef.current) {
      observer.observe(sectionRef.current);
    }

    return () => observer.disconnect();
  }, []);

  return (
    <section ref={sectionRef} className="py-20 lg:py-32 bg-white relative overflow-hidden">
      {/* Background decoration */}
      <div className="absolute top-0 right-0 w-96 h-96 bg-red-100 rounded-full blur-3xl opacity-30 -z-10" />
      <div className="absolute bottom-0 left-0 w-96 h-96 bg-yellow-100 rounded-full blur-3xl opacity-30 -z-10" />

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Section header */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-gray-900 mb-6">
            Powerful Features for{" "}
            <span className="bg-gradient-to-r from-red-600 to-yellow-600 bg-clip-text text-transparent">
              Data-Driven Insights
            </span>
          </h2>
          <p className="text-lg text-gray-600">
            Comprehensive tools and data to help you make informed decisions about internet connectivity in the
            Philippines.
          </p>
        </div>

        {/* Features grid */}
        <div className="grid md:grid-cols-2 gap-8">
          {features.map((feature, index) => {
            const Icon = feature.icon;
            const isVisible = visibleCards.includes(index);
            const colorClasses = {
              blue: {
                bg: "bg-blue-100",
                icon: "text-blue-600",
                gradient: "from-blue-500 to-blue-600",
                border: "border-blue-200",
              },
              purple: {
                bg: "bg-yellow-100",
                icon: "text-yellow-600",
                gradient: "from-yellow-500 to-yellow-600",
                border: "border-yellow-200",
              },
            };

            const colors = colorClasses[feature.color as keyof typeof colorClasses];

            return (
              <div
                key={index}
                className={`group relative transform transition-all duration-700 ${
                  isVisible ? "translate-y-0 opacity-100" : "translate-y-10 opacity-0"
                }`}>
                <div className="relative bg-white rounded-2xl p-8 border-2 border-gray-100 hover:border-gray-200 shadow-lg hover:shadow-xl transition-all duration-300">
                  {/* Icon */}
                  <div
                    className={`inline-flex p-4 rounded-xl ${colors.bg} mb-6 group-hover:scale-110 transition-transform duration-300`}>
                    <Icon className={`w-8 h-8 ${colors.icon}`} />
                  </div>

                  {/* Content */}
                  <h3 className="text-2xl font-bold text-gray-900 mb-4">{feature.title}</h3>
                  <p className="text-gray-600 leading-relaxed">{feature.description}</p>

                  {/* Hover effect gradient border */}
                  <div
                    className={`absolute inset-0 rounded-2xl bg-gradient-to-r ${colors.gradient} opacity-0 group-hover:opacity-10 transition-opacity duration-300 -z-10`}
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
