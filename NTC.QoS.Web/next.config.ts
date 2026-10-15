import type { NextConfig } from "next";

import "./src/env.js";

const config: NextConfig = {
  output: process.env.VERCEL ? undefined : "standalone",
  allowedDevOrigins: ["http://localhost:3000"],
  crossOrigin: "anonymous",

  images: { remotePatterns: [new URL("https://cloud.maptiler.com/static/img/maps/**")] },

  // Enable Turbopack support
  turbopack: {},

  // Enable Web Workers support with native Next.js handling
  webpack: (config) => {
    // Next.js 15+ supports Web Workers natively using new Worker(new URL(...))
    // No additional configuration needed, but we ensure proper module resolution
    config.resolve.fallback = { ...config.resolve.fallback, fs: false, net: false, tls: false };
    return config;
  },

  async headers() {
    const maxAge = 31536000; // 1 year for static assets
    return [
      {
        // Matches static assets in /assets/ directory recursively
        source: "/assets/:path*",
        headers: [{ key: "Cache-Control", value: `public, max-age=${maxAge}, immutable` }],
      },
      {
        // Matches all static media/font files across any directory
        source: "/:path*.(png|jpg|jpeg|gif|webp|svg|ico|woff|woff2)",
        headers: [{ key: "Cache-Control", value: `public, max-age=${maxAge}, immutable` }],
      },
    ];
  },
  cacheComponents: false,
  experimental: { mcpServer: true },
  reactStrictMode: false,
};

export default config;
