export default function MapIllustration() {
  return (
    <svg
      viewBox="0 0 500 500"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className="w-full h-full"
      aria-label="Interactive broadband coverage and connectivity map">
      <defs>
        {/* Gradient Definitions */}
        <linearGradient id="mapBg" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#fef3c7" />
          <stop offset="100%" stopColor="#fee2e2" />
        </linearGradient>
        <radialGradient id="coverageRed" cx="50%" cy="50%">
          <stop offset="0%" stopColor="#dc2626" stopOpacity="0.9" />
          <stop offset="50%" stopColor="#dc2626" stopOpacity="0.4" />
          <stop offset="100%" stopColor="#dc2626" stopOpacity="0" />
        </radialGradient>
        <radialGradient id="coverageYellow" cx="50%" cy="50%">
          <stop offset="0%" stopColor="#eab308" stopOpacity="0.9" />
          <stop offset="50%" stopColor="#eab308" stopOpacity="0.4" />
          <stop offset="100%" stopColor="#eab308" stopOpacity="0" />
        </radialGradient>
        <radialGradient id="coverageBlue" cx="50%" cy="50%">
          <stop offset="0%" stopColor="#2563eb" stopOpacity="0.9" />
          <stop offset="50%" stopColor="#2563eb" stopOpacity="0.4" />
          <stop offset="100%" stopColor="#2563eb" stopOpacity="0" />
        </radialGradient>
        <linearGradient id="dataFlow" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor="#dc2626" stopOpacity="0" />
          <stop offset="50%" stopColor="#dc2626" stopOpacity="1" />
          <stop offset="100%" stopColor="#dc2626" stopOpacity="0" />
        </linearGradient>
        <filter id="glow">
          <feGaussianBlur stdDeviation="3" result="coloredBlur" />
          <feMerge>
            <feMergeNode in="coloredBlur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
        <filter id="softShadow">
          <feDropShadow dx="0" dy="2" stdDeviation="4" floodOpacity="0.2" />
        </filter>

        {/* Animation Definitions */}
        <style>
          {`
                        @keyframes float {
                            0%, 100% { transform: translateY(0px); }
                            50% { transform: translateY(-10px); }
                        }
                        @keyframes expandRing {
                            0% { r: 8; opacity: 1; }
                            100% { r: 30; opacity: 0; }
                        }
                        @keyframes dataStream {
                            0% { stroke-dashoffset: 100; }
                            100% { stroke-dashoffset: 0; }
                        }
                        @keyframes markerPulse {
                            0%, 100% { transform: scale(1); opacity: 1; }
                            50% { transform: scale(1.2); opacity: 0.8; }
                        }
                        @keyframes gridFade {
                            0%, 100% { opacity: 0.1; }
                            50% { opacity: 0.3; }
                        }
                        @keyframes signalWave {
                            0% { d: path('M 0,0 Q 10,5 20,0 T 40,0'); }
                            50% { d: path('M 0,0 Q 10,-5 20,0 T 40,0'); }
                            100% { d: path('M 0,0 Q 10,5 20,0 T 40,0'); }
                        }
                        .float { animation: float 3s ease-in-out infinite; }
                        .expand-ring { animation: expandRing 2s ease-out infinite; }
                        .data-stream { animation: dataStream 3s linear infinite; }
                        .marker-pulse { animation: markerPulse 2s ease-in-out infinite; }
                        .grid-fade { animation: gridFade 4s ease-in-out infinite; }
                    `}
        </style>
      </defs>

      {/* Background */}
      <rect width="500" height="500" fill="url(#mapBg)" />

      {/* Topographic Grid Lines */}
      <g className="grid-fade" opacity="0.15">
        {Array.from({ length: 8 }).map((_, i) => (
          <g key={`topo-${i}`}>
            <ellipse
              cx="250"
              cy="250"
              rx={60 + i * 40}
              ry={50 + i * 35}
              stroke="#b91c1c"
              strokeWidth="1"
              fill="none"
              style={{ animationDelay: `${i * 0.2}s` }}
            />
          </g>
        ))}
      </g>

      {/* Map Grid Pattern */}
      <g opacity="0.1">
        {Array.from({ length: 10 }).map((_, i) => (
          <line key={`v-grid-${i}`} x1={50 * i} y1="0" x2={50 * i} y2="500" stroke="#991b1b" strokeWidth="0.5" />
        ))}
        {Array.from({ length: 10 }).map((_, i) => (
          <line key={`h-grid-${i}`} x1="0" y1={50 * i} x2="500" y2={50 * i} stroke="#991b1b" strokeWidth="0.5" />
        ))}
      </g>

      {/* Coverage Heat Zones */}
      <g opacity="0.5">
        <ellipse cx="180" cy="150" rx="100" ry="80" fill="url(#coverageRed)" className="float" />
        <ellipse
          cx="320"
          cy="220"
          rx="90"
          ry="90"
          fill="url(#coverageYellow)"
          className="float"
          style={{ animationDelay: "0.7s" } as any}
        />
        <ellipse
          cx="250"
          cy="350"
          rx="110"
          ry="70"
          fill="url(#coverageBlue)"
          className="float"
          style={{ animationDelay: "1.4s" } as any}
        />
        <ellipse
          cx="150"
          cy="300"
          rx="70"
          ry="70"
          fill="url(#coverageRed)"
          className="float"
          style={{ animationDelay: "2.1s" } as any}
        />
        <ellipse
          cx="380"
          cy="140"
          rx="80"
          ry="60"
          fill="url(#coverageYellow)"
          className="float"
          style={{ animationDelay: "2.8s" } as any}
        />
      </g>

      {/* Network Connection Lines */}
      <g opacity="0.4" filter="url(#glow)">
        <path
          d="M180 150 Q250 200 320 220"
          stroke="#dc2626"
          strokeWidth="2"
          fill="none"
          strokeDasharray="100"
          className="data-stream"
        />
        <path
          d="M320 220 Q285 285 250 350"
          stroke="#eab308"
          strokeWidth="2"
          fill="none"
          strokeDasharray="100"
          className="data-stream"
          style={{ animationDelay: "0.5s" } as any}
        />
        <path
          d="M180 150 Q165 225 150 300"
          stroke="#2563eb"
          strokeWidth="2"
          fill="none"
          strokeDasharray="100"
          className="data-stream"
          style={{ animationDelay: "1s" } as any}
        />
        <path
          d="M320 220 Q350 180 380 140"
          stroke="#dc2626"
          strokeWidth="2"
          fill="none"
          strokeDasharray="100"
          className="data-stream"
          style={{ animationDelay: "1.5s" } as any}
        />
        <path
          d="M150 300 Q200 325 250 350"
          stroke="#eab308"
          strokeWidth="2"
          fill="none"
          strokeDasharray="100"
          className="data-stream"
          style={{ animationDelay: "2s" } as any}
        />
      </g>

      {/* Coverage Towers / Cell Sites */}
      <g filter="url(#softShadow)">
        {[
          { x: 180, y: 150, color: "#dc2626", delay: 0 },
          { x: 320, y: 220, color: "#eab308", delay: 0.3 },
          { x: 250, y: 350, color: "#2563eb", delay: 0.6 },
          { x: 150, y: 300, color: "#dc2626", delay: 0.9 },
          { x: 380, y: 140, color: "#eab308", delay: 1.2 },
          { x: 280, y: 280, color: "#dc2626", delay: 1.5 },
          { x: 220, y: 180, color: "#2563eb", delay: 1.8 },
          { x: 100, y: 180, color: "#eab308", delay: 2.1 },
          { x: 400, y: 280, color: "#dc2626", delay: 2.4 },
        ].map((tower, i) => (
          <g key={`tower-${i}`}>
            {/* Expanding Ring */}
            <circle
              cx={tower.x}
              cy={tower.y}
              r="8"
              fill="none"
              stroke={tower.color}
              strokeWidth="2"
              opacity="0.6"
              className="expand-ring"
              style={{ animationDelay: `${tower.delay}s` } as any}
            />
            {/* Tower Base */}
            <g
              className="marker-pulse"
              style={{ animationDelay: `${tower.delay}s`, transformOrigin: `${tower.x}px ${tower.y}px` } as any}>
              <circle cx={tower.x} cy={tower.y} r="8" fill={tower.color} opacity="0.9" />
              <circle cx={tower.x} cy={tower.y} r="5" fill="white" />
              <circle cx={tower.x} cy={tower.y} r="2" fill={tower.color} />
            </g>
            {/* Signal Lines */}
            <g opacity="0.6">
              <line
                x1={tower.x - 6}
                y1={tower.y - 10}
                x2={tower.x - 6}
                y2={tower.y - 15}
                stroke={tower.color}
                strokeWidth="1.5"
                strokeLinecap="round"
              />
              <line
                x1={tower.x}
                y1={tower.y - 12}
                x2={tower.x}
                y2={tower.y - 18}
                stroke={tower.color}
                strokeWidth="1.5"
                strokeLinecap="round"
              />
              <line
                x1={tower.x + 6}
                y1={tower.y - 10}
                x2={tower.x + 6}
                y2={tower.y - 15}
                stroke={tower.color}
                strokeWidth="1.5"
                strokeLinecap="round"
              />
            </g>
          </g>
        ))}
      </g>

      {/* Data Flow Particles */}
      <g opacity="0.7">
        {Array.from({ length: 12 }).map((_, i) => {
          const angle = (i * 30 * Math.PI) / 180;
          const radius = 120 + (i % 3) * 40;
          const cx = Math.round((250 + radius * Math.cos(angle)) * 1000) / 1000;
          const cy = Math.round((250 + radius * Math.sin(angle)) * 1000) / 1000;
          return (
            <circle
              key={`particle-${i}`}
              cx={cx}
              cy={cy}
              r="2"
              fill={i % 3 === 0 ? "#dc2626" : i % 3 === 1 ? "#eab308" : "#2563eb"}
              className="float"
              style={{ animationDelay: `${i * 0.2}s` } as any}
            />
          );
        })}
      </g>

      {/* Interactive Hotspots */}
      <g opacity="0.3">
        <circle
          cx="180"
          cy="150"
          r="60"
          fill="none"
          stroke="#dc2626"
          strokeWidth="1.5"
          strokeDasharray="5 5"
          className="marker-pulse"
        />
        <circle
          cx="320"
          cy="220"
          r="55"
          fill="none"
          stroke="#eab308"
          strokeWidth="1.5"
          strokeDasharray="5 5"
          className="marker-pulse"
          style={{ animationDelay: "0.5s" } as any}
        />
        <circle
          cx="250"
          cy="350"
          r="65"
          fill="none"
          stroke="#2563eb"
          strokeWidth="1.5"
          strokeDasharray="5 5"
          className="marker-pulse"
          style={{ animationDelay: "1s" } as any}
        />
      </g>

      {/* Central Hub Indicator */}
      <g filter="url(#glow)">
        <circle cx="250" cy="250" r="15" fill="#ffffff" opacity="0.9" className="marker-pulse" />
        <circle cx="250" cy="250" r="10" fill="#dc2626" />
        <circle cx="250" cy="250" r="5" fill="#ffffff" />
        <text x="250" y="230" textAnchor="middle" fill="#991b1b" fontSize="10" fontWeight="600">
          HUB
        </text>
      </g>

      {/* Region Labels */}
      <g opacity="0.6" fontSize="9" fill="#991b1b" fontWeight="500">
        <text x="180" y="130" textAnchor="middle">
          NCR
        </text>
        <text x="320" y="200" textAnchor="middle">
          LUZON
        </text>
        <text x="250" y="330" textAnchor="middle">
          VISAYAS
        </text>
        <text x="150" y="280" textAnchor="middle">
          MINDANAO
        </text>
        <text x="380" y="120" textAnchor="middle">
          REGION
        </text>
      </g>
    </svg>
  );
}
