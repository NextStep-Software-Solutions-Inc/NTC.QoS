"use client";

import { useEffect, useRef } from "react";

export default function NetworkIllustration() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const resizeCanvas = () => {
      canvas.width = canvas.offsetWidth * window.devicePixelRatio;
      canvas.height = canvas.offsetHeight * window.devicePixelRatio;
      ctx.scale(window.devicePixelRatio, window.devicePixelRatio);
    };

    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);

    interface Wave {
      x: number;
      y: number;
      radius: number;
      maxRadius: number;
      speed: number;
      opacity: number;
      color: string;
    }

    const waves: Wave[] = [];
    const width = canvas.offsetWidth;
    const height = canvas.offsetHeight;
    let time = 0;

    // Create signal towers
    const towers = [
      { x: width * 0.2, y: height * 0.3 },
      { x: width * 0.5, y: height * 0.5 },
      { x: width * 0.8, y: height * 0.4 },
      { x: width * 0.35, y: height * 0.7 },
      { x: width * 0.65, y: height * 0.25 },
    ];

    const colors = [
      "220, 38, 38", // red
      "234, 179, 8", // yellow
      "37, 99, 235", // blue
    ];

    // Create waves periodically
    const createWave = () => {
      const tower = towers[Math.floor(Math.random() * towers.length)];
      const color = colors[Math.floor(Math.random() * colors.length)];

      if (!tower || !color) return;

      waves.push({
        x: tower.x,
        y: tower.y,
        radius: 0,
        maxRadius: 120 + Math.random() * 80,
        speed: 0.8 + Math.random() * 0.5,
        opacity: 1,
        color,
      });
    };

    setInterval(createWave, 800);

    const animate = () => {
      ctx.clearRect(0, 0, width, height);
      time += 0.02;

      // Draw grid lines
      ctx.strokeStyle = "rgba(220, 38, 38, 0.05)";
      ctx.lineWidth = 1;
      for (let i = 0; i < width; i += 60) {
        ctx.beginPath();
        ctx.moveTo(i, 0);
        ctx.lineTo(i, height);
        ctx.stroke();
      }
      for (let i = 0; i < height; i += 60) {
        ctx.beginPath();
        ctx.moveTo(0, i);
        ctx.lineTo(width, i);
        ctx.stroke();
      }

      // Draw signal towers
      towers.forEach((tower, index) => {
        // Tower base
        ctx.fillStyle = `rgba(220, 38, 38, 0.8)`;
        ctx.beginPath();
        ctx.arc(tower.x, tower.y, 6, 0, Math.PI * 2);
        ctx.fill();

        // Tower antenna with pulsing effect
        const pulse = Math.sin(time * 2 + index) * 0.3 + 0.7;
        ctx.fillStyle = `rgba(234, 179, 8, ${pulse})`;
        ctx.beginPath();
        ctx.arc(tower.x, tower.y, 3, 0, Math.PI * 2);
        ctx.fill();

        // Signal beams
        ctx.save();
        ctx.translate(tower.x, tower.y);
        for (let i = 0; i < 3; i++) {
          const angle = (time + (i * (Math.PI * 2)) / 3) % (Math.PI * 2);
          const beamLength = 40 + Math.sin(time * 3 + i) * 10;

          ctx.strokeStyle = `rgba(37, 99, 235, ${0.4 - i * 0.1})`;
          ctx.lineWidth = 2;
          ctx.beginPath();
          ctx.moveTo(0, 0);
          ctx.lineTo(Math.cos(angle) * beamLength, Math.sin(angle) * beamLength);
          ctx.stroke();
        }
        ctx.restore();
      });

      // Update and draw waves
      for (let i = waves.length - 1; i >= 0; i--) {
        const wave = waves[i];
        if (!wave) continue;

        wave.radius += wave.speed;
        wave.opacity = 1 - wave.radius / wave.maxRadius;

        if (wave.opacity <= 0) {
          waves.splice(i, 1);
          continue;
        }

        ctx.strokeStyle = `rgba(${wave.color}, ${wave.opacity * 0.5})`;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(wave.x, wave.y, wave.radius, 0, Math.PI * 2);
        ctx.stroke();

        // Inner wave
        if (wave.radius > 20) {
          ctx.strokeStyle = `rgba(${wave.color}, ${wave.opacity * 0.3})`;
          ctx.lineWidth = 1;
          ctx.beginPath();
          ctx.arc(wave.x, wave.y, wave.radius - 10, 0, Math.PI * 2);
          ctx.stroke();
        }
      }

      // Draw data packets traveling between towers
      towers.forEach((tower, i) => {
        if (i < towers.length - 1) {
          const next = towers[i + 1];
          if (!next) return;

          const progress = (Math.sin(time + i) + 1) / 2;
          const x = tower.x + (next.x - tower.x) * progress;
          const y = tower.y + (next.y - tower.y) * progress;

          ctx.fillStyle = `rgba(234, 179, 8, 0.8)`;
          ctx.beginPath();
          ctx.arc(x, y, 3, 0, Math.PI * 2);
          ctx.fill();

          // Packet glow
          ctx.fillStyle = `rgba(234, 179, 8, 0.2)`;
          ctx.beginPath();
          ctx.arc(x, y, 8, 0, Math.PI * 2);
          ctx.fill();
        }
      });

      requestAnimationFrame(animate);
    };

    animate();
    createWave();

    return () => {
      window.removeEventListener("resize", resizeCanvas);
    };
  }, []);

  return <canvas ref={canvasRef} className="absolute inset-0 w-full h-full" aria-hidden="true" />;
}
