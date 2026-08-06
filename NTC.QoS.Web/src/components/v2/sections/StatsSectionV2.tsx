"use client";

import { useEffect, useRef, useState } from "react";
import { Activity, Globe, Signal, Users } from "lucide-react";

const stats = [
  { icon: Globe, value: "50,000+", label: "Cell Sites Mapped", color: "blue", suffix: "" },
  { icon: Activity, value: "1.2M+", label: "Speed Tests Recorded", color: "purple", suffix: "" },
  { icon: Signal, value: "98.5", label: "Coverage Rate", color: "blue", suffix: "%" },
  { icon: Users, value: "100K+", label: "Monthly Active Users", color: "purple", suffix: "" },
];

export default function StatsSectionV2() {
  const [isVisible, setIsVisible] = useState(false);
  const [animatedValues, setAnimatedValues] = useState<Record<number, number>>({});
  const sectionRef = useRef<HTMLElement>(null);

  // Move animateCounters before useEffect
  const animateCounters = () => {
    stats.forEach((stat, index) => {
      const targetValue = parseFloat(stat.value.replace(/[^0-9.]/g, ""));
      const duration = 2000;
      const steps = 60;
      const increment = targetValue / steps;
      let currentValue = 0;

      const interval = setInterval(() => {
        currentValue += increment;
        if (currentValue >= targetValue) {
          currentValue = targetValue;
          clearInterval(interval);
        }
        setAnimatedValues((prev) => ({ ...prev, [index]: currentValue }));
      }, duration / steps);
    });
  };

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && !isVisible) {
            setIsVisible(true);
            animateCounters();
          }
        });
      },
      { threshold: 0.3 },
    );

    if (sectionRef.current) {
      observer.observe(sectionRef.current);
    }

    return () => observer.disconnect();
  }, [isVisible, animateCounters]);

  const formatValue = (value: number, originalValue: string) => {
    if (originalValue.includes("K")) {
      return `${Math.floor(value / 1000)}K+`;
    } else if (originalValue.includes("M")) {
      return `${(value / 1000000).toFixed(1)}M+`;
    } else if (originalValue.includes(".")) {
      return value.toFixed(1);
    }
    return Math.floor(value).toLocaleString();
  };

  return (
    <section
      ref={sectionRef}
      className="py-20 lg:py-32 bg-gradient-to-br from-red-600 via-red-700 to-blue-700 relative overflow-hidden">
      {/* Background pattern */}
      <div className="absolute inset-0 opacity-10">
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          }}
        />
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        {/* Section header */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-6">
            Trusted by Millions of Filipinos
          </h2>
          <p className="text-lg text-yellow-100">
            Real-time data and insights powering informed decisions about internet connectivity across the archipelago.
          </p>
        </div>

        {/* Stats grid */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-8">
          {stats.map((stat, index) => {
            const Icon = stat.icon;
            const animatedValue = animatedValues[index] || 0;

            return (
              <div
                key={index}
                className={`text-center transform transition-all duration-700 delay-${index * 100} ${
                  isVisible ? "translate-y-0 opacity-100" : "translate-y-10 opacity-0"
                }`}>
                <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-8 border border-white/20 hover:bg-white/15 transition-all duration-300">
                  {/* Icon */}
                  <div className="inline-flex p-4 bg-white/10 rounded-xl mb-4">
                    <Icon className="w-8 h-8 text-white" />
                  </div>

                  {/* Value */}
                  <div className="text-4xl lg:text-5xl font-bold text-white mb-2">
                    {isVisible ? formatValue(animatedValue, stat.value) : "0"}
                    {stat.suffix && <span className="text-2xl">{stat.suffix}</span>}
                  </div>

                  {/* Label */}
                  <div className="text-yellow-100 font-medium">{stat.label}</div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Bottom CTA */}
        <div className="mt-16 text-center">
          <p className="text-yellow-100 text-lg mb-6">
            Join thousands of users making data-driven connectivity decisions
          </p>
          <button className="inline-flex items-center px-8 py-4 bg-white text-red-700 rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transform transition-all duration-200">
            Get Started Today
          </button>
        </div>
      </div>
    </section>
  );
}
