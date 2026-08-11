export default function DataFlowIllustration() {
  return (
    <svg
      viewBox="0 0 400 300"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className="w-full h-full"
      aria-label="Data flow and network traffic visualization">
      <defs>
        <linearGradient id="flowGradient" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor="#dc2626" />
          <stop offset="50%" stopColor="#eab308" />
          <stop offset="100%" stopColor="#2563eb" />
        </linearGradient>
        <linearGradient id="circleGradient" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#dc2626" />
          <stop offset="100%" stopColor="#eab308" />
        </linearGradient>
        <filter id="glow">
          <feGaussianBlur stdDeviation="4" result="coloredBlur" />
          <feMerge>
            <feMergeNode in="coloredBlur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>

      {/* Central hub */}
      <g filter="url(#glow)">
        <circle cx="200" cy="150" r="30" fill="url(#circleGradient)" opacity="0.3" className="animate-pulse" />
        <circle cx="200" cy="150" r="20" stroke="#dc2626" strokeWidth="3" fill="none" />
        <circle cx="200" cy="150" r="8" fill="#eab308" className="animate-pulse" />
      </g>

      {/* Orbiting nodes */}
      <g>
        {[0, 60, 120, 180, 240, 300].map((angle, i) => {
          const radian = (angle * Math.PI) / 180;
          const radius = 100;
          const x = 200 + Math.cos(radian) * radius;
          const y = 150 + Math.sin(radian) * radius;

          return (
            <g key={`orbit-${i}`}>
              {/* Orbit path */}
              <circle
                cx="200"
                cy="150"
                r={radius}
                stroke={i % 2 === 0 ? "#dc2626" : "#eab308"}
                strokeWidth="1"
                fill="none"
                opacity="0.1"
                strokeDasharray="5 10"
              />

              {/* Node */}
              <circle
                cx={x}
                cy={y}
                r="8"
                fill={i % 2 === 0 ? "#dc2626" : "#eab308"}
                className="animate-pulse"
                style={{ animationDelay: `${i * 0.15}s` }}
              />
              <circle cx={x} cy={y} r="4" fill="white" opacity="0.8" />

              {/* Connecting line with gradient */}
              <line
                x1="200"
                y1="150"
                x2={x}
                y2={y}
                stroke="url(#flowGradient)"
                strokeWidth="1.5"
                opacity="0.3"
                strokeDasharray="4 4"
              />
            </g>
          );
        })}
      </g>

      {/* Data packets flowing */}
      <g>
        {[0, 120, 240].map((startAngle, i) => {
          const angles = Array.from({ length: 8 }, (_, j) => startAngle + j * 15);
          return angles.map((angle, j) => {
            const radian = (angle * Math.PI) / 180;
            const radius = 60 + (j % 2) * 20;
            const x = 200 + Math.cos(radian) * radius;
            const y = 150 + Math.sin(radian) * radius;

            return (
              <circle
                key={`packet-${i}-${j}`}
                cx={x}
                cy={y}
                r="2"
                fill={i % 2 === 0 ? "#dc2626" : "#2563eb"}
                opacity="0.6"
                className="animate-pulse"
                style={{ animationDelay: `${(i + j) * 0.1}s` }}
              />
            );
          });
        })}
      </g>

      {/* Outer ring */}
      <circle
        cx="200"
        cy="150"
        r="140"
        stroke="url(#circleGradient)"
        strokeWidth="2"
        fill="none"
        opacity="0.2"
        strokeDasharray="10 5"
        className="animate-spin"
        style={{ animationDuration: "20s" }}
      />

      {/* Speed indicators */}
      {[45, 135, 225, 315].map((angle, i) => {
        const radian = (angle * Math.PI) / 180;
        const x1 = 200 + Math.cos(radian) * 145;
        const y1 = 150 + Math.sin(radian) * 145;
        const x2 = 200 + Math.cos(radian) * 160;
        const y2 = 150 + Math.sin(radian) * 160;

        return (
          <line
            key={`indicator-${i}`}
            x1={x1}
            y1={y1}
            x2={x2}
            y2={y2}
            stroke="#dc2626"
            strokeWidth="3"
            opacity="0.5"
            strokeLinecap="round"
            className="animate-pulse"
            style={{ animationDelay: `${i * 0.2}s` }}
          />
        );
      })}
    </svg>
  );
}
