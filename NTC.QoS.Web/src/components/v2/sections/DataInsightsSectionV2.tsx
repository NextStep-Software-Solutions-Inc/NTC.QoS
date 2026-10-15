"use client";

import { useEffect, useRef, useState } from "react";
import { Building2, Signal, Wifi, Zap } from "lucide-react";

import DataFlowIllustration from "../illustrations/DataFlowIllustration";

const insights = [
  {
    icon: Signal,
    title: "Real-Time Monitoring",
    description: "Live dashboard with real-time network performance metrics and system health indicators",
    stat: "24/7",
    label: "Monitoring",
    color: "blue",
  },
  {
    icon: Wifi,
    title: "Interactive Maps",
    description: "Advanced coverage mapping with interactive visualization of network infrastructure",
    stat: "Interactive",
    label: "Mapping",
    color: "purple",
  },
  {
    icon: Zap,
    title: "Performance Analytics",
    description: "Comprehensive analytics dashboard with speed test data, user metrics, and trend analysis",
    stat: "Advanced",
    label: "Analytics",
    color: "blue",
  },
  {
    icon: Building2,
    title: "System Management",
    description: "Complete administrative control over users, permissions, and system configuration",
    stat: "Full",
    label: "Control",
    color: "purple",
  },
];

export default function DataInsightsSectionV2() {
  const [isVisible, setIsVisible] = useState(false);
  const sectionRef = useRef<HTMLElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsVisible(true);
          }
        });
      },
      { threshold: 0.2 },
    );

    if (sectionRef.current) {
      observer.observe(sectionRef.current);
    }

    return () => observer.disconnect();
  }, []);

  return (
    <section
      ref={sectionRef}
      className="py-20 lg:py-32 bg-gradient-to-br from-gray-900 to-blue-900 relative overflow-hidden">
      {/* Background pattern */}
      <div className="absolute inset-0 opacity-5">
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
            Powerful{" "}
            <span className="bg-gradient-to-r from-yellow-400 to-red-400 bg-clip-text text-transparent">
              Admin Dashboard
            </span>{" "}
            Capabilities
          </h2>
          <p className="text-lg text-gray-300">
            Comprehensive administrative tools for managing telecommunications data, monitoring system performance, and
            overseeing network infrastructure
          </p>
        </div>

        <div className="grid lg:grid-cols-2 gap-12 items-center">
          {/* Insights cards */}
          <div className="space-y-6">
            {insights.map((insight, index) => {
              const Icon = insight.icon;
              const colorClasses = {
                blue: { bg: "bg-blue-500/10", text: "text-blue-400", border: "border-blue-500/20" },
                purple: { bg: "bg-yellow-500/10", text: "text-yellow-400", border: "border-yellow-500/20" },
              };

              const colors = colorClasses[insight.color as keyof typeof colorClasses];

              return (
                <div
                  key={index}
                  className={`transform transition-all duration-700 delay-${index * 100} ${
                    isVisible ? "translate-x-0 opacity-100" : "-translate-x-10 opacity-0"
                  }`}>
                  <div
                    className={`bg-white/5 backdrop-blur-sm border-2 ${colors.border} rounded-xl p-6 hover:bg-white/10 transition-all duration-300`}>
                    <div className="flex items-start gap-4">
                      <div className={`${colors.bg} p-3 rounded-lg`}>
                        <Icon className={`w-6 h-6 ${colors.text}`} />
                      </div>

                      <div className="flex-1">
                        <h3 className="text-xl font-bold text-white mb-2">{insight.title}</h3>
                        <p className="text-gray-300 text-sm mb-4">{insight.description}</p>

                        <div className="flex items-baseline gap-2">
                          <span className={`text-2xl font-bold ${colors.text}`}>{insight.stat}</span>
                          <span className="text-gray-400 text-sm">{insight.label}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Illustration */}
          <div
            className={`relative transform transition-all duration-1000 delay-300 ${
              isVisible ? "translate-y-0 opacity-100" : "translate-y-10 opacity-0"
            }`}>
            <div className="relative bg-white/5 backdrop-blur-sm border-2 border-white/10 rounded-3xl p-8">
              <div className="aspect-[4/3] flex items-center justify-center">
                <DataFlowIllustration />
              </div>

              {/* Floating stat badges */}
              <div className="absolute -top-4 -right-4 bg-red-600 rounded-xl shadow-lg px-4 py-3 animate-float">
                <div className="text-xs text-red-100">Dashboard</div>
                <div className="text-lg font-bold text-white">Modules</div>
              </div>

              <div className="absolute -bottom-4 -left-4 bg-yellow-600 rounded-xl shadow-lg px-4 py-3 animate-float animation-delay-2000">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
                  <span className="text-sm font-semibold text-white">Live Updates</span>
                </div>
              </div>
            </div>

            {/* Glow effect */}
            <div className="absolute -inset-4 bg-gradient-to-r from-red-600/20 to-yellow-600/20 rounded-3xl blur-2xl -z-10" />
          </div>
        </div>
      </div>
    </section>
  );
}
