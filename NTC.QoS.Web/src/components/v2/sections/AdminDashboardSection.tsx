"use client";

import { useEffect, useRef, useState } from "react";
import { Building2, Signal, Wifi, Zap } from "lucide-react";

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

export default function AdminDashboardSection() {
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

  const getDashboardContent = () => {
    return (
      <svg viewBox="0 0 500 350" className="w-full h-full" fill="none" xmlns="http://www.w3.org/2000/svg">
        <defs>
          {/* Gradient definitions */}
          <linearGradient id="darkBg" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#0f172a" />
            <stop offset="100%" stopColor="#1e293b" />
          </linearGradient>
          <linearGradient id="cardShine" x1="0%" y1="0%" x2="0%" y2="100%">
            <stop offset="0%" stopColor="#1e293b" stopOpacity="0.8" />
            <stop offset="100%" stopColor="#0f172a" stopOpacity="0.9" />
          </linearGradient>
          <linearGradient id="greenGlow" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#10b981" />
            <stop offset="100%" stopColor="#34d399" />
          </linearGradient>
          <linearGradient id="blueGlow" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#3b82f6" />
            <stop offset="100%" stopColor="#60a5fa" />
          </linearGradient>
          <linearGradient id="purpleGlow" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#8b5cf6" />
            <stop offset="100%" stopColor="#a78bfa" />
          </linearGradient>
          <filter id="glow">
            <feGaussianBlur stdDeviation="2" result="coloredBlur" />
            <feMerge>
              <feMergeNode in="coloredBlur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <filter id="shadow">
            <feDropShadow dx="0" dy="2" stdDeviation="3" floodOpacity="0.3" />
          </filter>

          {/* Animation definitions */}
          <style>
            {`
                            @keyframes breathe {
                                0%, 100% { opacity: 0.6; transform: scale(1); }
                                50% { opacity: 1; transform: scale(1.05); }
                            }
                            @keyframes waveMove {
                                0% { transform: translateX(0); }
                                100% { transform: translateX(-50px); }
                            }
                            @keyframes lineGrow {
                                0% { stroke-dashoffset: 200; }
                                100% { stroke-dashoffset: 0; }
                            }
                            @keyframes dotFloat {
                                0%, 100% { transform: translateY(0); }
                                50% { transform: translateY(-8px); }
                            }
                            @keyframes ringPulse {
                                0% { r: 3; opacity: 1; }
                                100% { r: 12; opacity: 0; }
                            }
                            @keyframes rotateGlobe {
                                0% { transform: rotate(0deg); }
                                100% { transform: rotate(360deg); }
                            }
                            @keyframes numberTick {
                                0%, 100% { opacity: 1; }
                                50% { opacity: 0.7; }
                            }
                            @keyframes progressFill {
                                0% { width: 0; }
                                100% { width: var(--progress-width); }
                            }
                            .breathe { animation: breathe 3s ease-in-out infinite; }
                            .wave-move { animation: waveMove 4s linear infinite; }
                            .line-grow { animation: lineGrow 2s ease-out forwards; }
                            .dot-float { animation: dotFloat 2s ease-in-out infinite; }
                            .ring-pulse { animation: ringPulse 2s ease-out infinite; }
                            .rotate-globe { animation: rotateGlobe 20s linear infinite; }
                            .number-tick { animation: numberTick 1.5s ease-in-out infinite; }
                            .progress-fill { animation: progressFill 1.5s ease-out forwards; }
                        `}
          </style>
        </defs>

        {/* Main Background */}
        <rect width="500" height="350" rx="16" fill="url(#darkBg)" />

        {/* Grid Pattern Background */}
        <g opacity="0.1">
          {Array.from({ length: 10 }).map((_, i) => (
            <line key={`v${i}`} x1={50 * i} y1="0" x2={50 * i} y2="350" stroke="#64748b" strokeWidth="0.5" />
          ))}
          {Array.from({ length: 7 }).map((_, i) => (
            <line key={`h${i}`} x1="0" y1={50 * i} x2="500" y2={50 * i} stroke="#64748b" strokeWidth="0.5" />
          ))}
        </g>

        {/* Top Navigation Bar */}
        <g filter="url(#shadow)">
          <rect x="15" y="15" width="470" height="45" rx="8" fill="url(#cardShine)" opacity="0.95" />
          <circle cx="30" cy="37.5" r="5" fill="#10b981" className="breathe" />
          <text x="45" y="42" fill="#e2e8f0" fontSize="16" fontWeight="bold">
            NTC QoS Dashboard
          </text>
          <text x="430" y="42" fill="#64748b" fontSize="11">
            LIVE
          </text>
          <circle cx="467" cy="37.5" r="3" fill="#ef4444" className="breathe" />
        </g>

        {/* Left Column - Stats Cards */}
        <g transform="translate(15, 75)">
          {/* Card 1 - Network Status */}
          <g filter="url(#shadow)">
            <rect width="145" height="85" rx="8" fill="url(#cardShine)" />
            <circle cx="20" cy="20" r="8" fill="url(#greenGlow)" className="breathe" />
            <circle
              cx="20"
              cy="20"
              r="12"
              fill="none"
              stroke="#10b981"
              strokeWidth="1"
              opacity="0.3"
              className="ring-pulse"
            />
            <text x="35" y="25" fill="#94a3b8" fontSize="10">
              Network
            </text>
            <text x="20" y="50" fill="#e2e8f0" fontSize="22" fontWeight="bold" className="number-tick">
              99.9%
            </text>
            <text x="20" y="68" fill="#10b981" fontSize="9">
              Uptime
            </text>
            <rect x="20" y="73" width="105" height="4" rx="2" fill="#1e293b" />
            <rect
              x="20"
              y="73"
              width="102"
              height="4"
              rx="2"
              fill="url(#greenGlow)"
              className="progress-fill"
              style={{ "--progress-width": "102px" } as any}
            />
          </g>

          {/* Card 2 - Active Users */}
          <g transform="translate(0, 95)" filter="url(#shadow)">
            <rect width="145" height="85" rx="8" fill="url(#cardShine)" />
            <circle
              cx="20"
              cy="20"
              r="8"
              fill="url(#blueGlow)"
              className="breathe"
              style={{ animationDelay: "0.5s" } as any}
            />
            <text x="35" y="25" fill="#94a3b8" fontSize="10">
              Active Users
            </text>
            <text x="20" y="50" fill="#e2e8f0" fontSize="22" fontWeight="bold" className="number-tick">
              2,847
            </text>
            <text x="20" y="68" fill="#3b82f6" fontSize="9">
              +12.5%
            </text>
            <g className="dot-float">
              <circle cx="115" cy="45" r="3" fill="#3b82f6" opacity="0.6" />
            </g>
            <g className="dot-float" style={{ animationDelay: "0.3s" } as any}>
              <circle cx="125" cy="50" r="3" fill="#60a5fa" opacity="0.6" />
            </g>
            <g className="dot-float" style={{ animationDelay: "0.6s" } as any}>
              <circle cx="135" cy="42" r="3" fill="#93c5fd" opacity="0.6" />
            </g>
          </g>

          {/* Card 3 - Speed Tests */}
          <g transform="translate(0, 190)" filter="url(#shadow)">
            <rect width="145" height="75" rx="8" fill="url(#cardShine)" />
            <circle
              cx="20"
              cy="20"
              r="8"
              fill="url(#purpleGlow)"
              className="breathe"
              style={{ animationDelay: "1s" } as any}
            />
            <text x="35" y="25" fill="#94a3b8" fontSize="10">
              Speed Tests
            </text>
            <text x="20" y="50" fill="#e2e8f0" fontSize="22" fontWeight="bold" className="number-tick">
              156K
            </text>
            <text x="20" y="65" fill="#8b5cf6" fontSize="9">
              Today
            </text>
          </g>
        </g>

        {/* Center Column - Main Chart */}
        <g transform="translate(175, 75)" filter="url(#shadow)">
          <rect width="160" height="190" rx="8" fill="url(#cardShine)" />
          <text x="15" y="20" fill="#e2e8f0" fontSize="11" fontWeight="600">
            Performance Trends
          </text>

          {/* Waveform Chart */}
          <g transform="translate(15, 35)">
            <path
              d="M0,100 Q20,60 40,70 T80,80 T120,50 T160,70"
              stroke="url(#greenGlow)"
              strokeWidth="2.5"
              fill="none"
              className="line-grow"
              strokeDasharray="200"
              filter="url(#glow)"
            />
            <path
              d="M0,110 Q20,80 40,90 T80,95 T120,75 T160,90"
              stroke="url(#blueGlow)"
              strokeWidth="2"
              fill="none"
              className="line-grow"
              strokeDasharray="200"
              style={{ animationDelay: "0.3s" } as any}
              opacity="0.7"
            />
            <path
              d="M0,120 Q20,95 40,105 T80,110 T120,95 T160,105"
              stroke="url(#purpleGlow)"
              strokeWidth="2"
              fill="none"
              className="line-grow"
              strokeDasharray="200"
              style={{ animationDelay: "0.6s" } as any}
              opacity="0.5"
            />

            {/* Data Points */}
            <circle cx="40" cy="70" r="3" fill="#10b981" className="breathe" filter="url(#glow)" />
            <circle
              cx="80"
              cy="80"
              r="3"
              fill="#10b981"
              className="breathe"
              style={{ animationDelay: "0.5s" } as any}
              filter="url(#glow)"
            />
            <circle
              cx="120"
              cy="50"
              r="3"
              fill="#10b981"
              className="breathe"
              style={{ animationDelay: "1s" } as any}
              filter="url(#glow)"
            />
          </g>

          {/* Legend */}
          <g transform="translate(15, 160)">
            <circle cx="0" cy="0" r="3" fill="#10b981" />
            <text x="8" y="3" fill="#94a3b8" fontSize="8">
              Bandwidth
            </text>
            <circle cx="60" cy="0" r="3" fill="#3b82f6" />
            <text x="68" y="3" fill="#94a3b8" fontSize="8">
              Latency
            </text>
            <circle cx="115" cy="0" r="3" fill="#8b5cf6" />
            <text x="123" y="3" fill="#94a3b8" fontSize="8">
              Load
            </text>
          </g>
        </g>

        {/* Right Column - Map & Metrics */}
        <g transform="translate(350, 75)">
          {/* Interactive Map */}
          <g filter="url(#shadow)">
            <rect width="135" height="120" rx="8" fill="url(#cardShine)" />
            <text x="15" y="20" fill="#e2e8f0" fontSize="11" fontWeight="600">
              Coverage Map
            </text>

            {/* Globe/Map representation */}
            <g transform="translate(67.5, 70)" className="rotate-globe" style={{ transformOrigin: "center" } as any}>
              <circle r="25" fill="none" stroke="#334155" strokeWidth="1" opacity="0.3" />
              <ellipse rx="25" ry="8" fill="none" stroke="#334155" strokeWidth="1" opacity="0.3" />
              <ellipse rx="8" ry="25" fill="none" stroke="#334155" strokeWidth="1" opacity="0.3" />
            </g>

            {/* Cell Sites */}
            <g className="breathe">
              <circle cx="50" cy="55" r="3" fill="#10b981" opacity="0.8" />
              <circle
                cx="50"
                cy="55"
                r="6"
                fill="none"
                stroke="#10b981"
                strokeWidth="1"
                opacity="0.3"
                className="ring-pulse"
              />
            </g>
            <g className="breathe" style={{ animationDelay: "0.7s" } as any}>
              <circle cx="85" cy="65" r="3" fill="#3b82f6" opacity="0.8" />
              <circle
                cx="85"
                cy="65"
                r="6"
                fill="none"
                stroke="#3b82f6"
                strokeWidth="1"
                opacity="0.3"
                className="ring-pulse"
                style={{ animationDelay: "0.7s" } as any}
              />
            </g>
            <g className="breathe" style={{ animationDelay: "1.4s" } as any}>
              <circle cx="70" cy="85" r="3" fill="#f59e0b" opacity="0.8" />
              <circle
                cx="70"
                cy="85"
                r="6"
                fill="none"
                stroke="#f59e0b"
                strokeWidth="1"
                opacity="0.3"
                className="ring-pulse"
                style={{ animationDelay: "1.4s" } as any}
              />
            </g>
          </g>

          {/* Quick Stats */}
          <g transform="translate(0, 135)" filter="url(#shadow)">
            <rect width="135" height="130" rx="8" fill="url(#cardShine)" />
            <text x="15" y="20" fill="#e2e8f0" fontSize="11" fontWeight="600">
              System Status
            </text>

            <g transform="translate(15, 35)">
              <circle cx="0" cy="0" r="4" fill="#10b981" className="breathe" />
              <text x="12" y="4" fill="#e2e8f0" fontSize="9">
                Monitoring
              </text>
              <text x="95" y="4" fill="#10b981" fontSize="9">
                Active
              </text>
            </g>

            <g transform="translate(15, 55)">
              <circle
                cx="0"
                cy="0"
                r="4"
                fill="#3b82f6"
                className="breathe"
                style={{ animationDelay: "0.3s" } as any}
              />
              <text x="12" y="4" fill="#e2e8f0" fontSize="9">
                Analytics
              </text>
              <text x="95" y="4" fill="#3b82f6" fontSize="9">
                Running
              </text>
            </g>

            <g transform="translate(15, 75)">
              <circle
                cx="0"
                cy="0"
                r="4"
                fill="#8b5cf6"
                className="breathe"
                style={{ animationDelay: "0.6s" } as any}
              />
              <text x="12" y="4" fill="#e2e8f0" fontSize="9">
                Database
              </text>
              <text x="95" y="4" fill="#8b5cf6" fontSize="9">
                Healthy
              </text>
            </g>

            <g transform="translate(15, 95)">
              <circle
                cx="0"
                cy="0"
                r="4"
                fill="#f59e0b"
                className="breathe"
                style={{ animationDelay: "0.9s" } as any}
              />
              <text x="12" y="4" fill="#e2e8f0" fontSize="9">
                API
              </text>
              <text x="95" y="4" fill="#f59e0b" fontSize="9">
                99.9%
              </text>
            </g>
          </g>
        </g>

        {/* Floating Data Particles */}
        <g opacity="0.4">
          <circle cx="100" cy="290" r="2" fill="#10b981" className="dot-float" />
          <circle
            cx="250"
            cy="310"
            r="2"
            fill="#3b82f6"
            className="dot-float"
            style={{ animationDelay: "0.5s" } as any}
          />
          <circle
            cx="400"
            cy="300"
            r="2"
            fill="#8b5cf6"
            className="dot-float"
            style={{ animationDelay: "1s" } as any}
          />
        </g>
      </svg>
    );
  };

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
                {/* Animated Dashboard Illustration */}
                <div className="relative w-full h-full transition-all duration-1000 ease-in-out">
                  {getDashboardContent()}
                </div>
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
