# ⚡ NTC QoS Web Frontend (Next.js 16 + Bun)

> High-performance Web Application for NTC Quality of Service Monitoring & Analytics.

---

## 🛠️ Stack & Features

* **Framework:** Next.js 16 (App Router + Turbopack)
* **Runtime & Package Manager:** Bun 1.x
* **UI Library:** React 19 + TailwindCSS + Shadcn UI
* **Maps & GIS:** MapTiler SDK + Turf.js
* **State Management & Data Fetching:** TanStack React Query v5

---

## 🚀 Getting Started Locally

### Prerequisites
Install [Bun 1.x](https://bun.sh/) on your machine:
```bash
curl -fsSL https://bun.sh/install | bash  # macOS/Linux
powershell -c "irm bun.sh/install.ps1 | iex" # Windows
```

### Development Commands

```bash
# 1. Install dependencies
bun install

# 2. Run local development server
bun dev

# 3. Build production standalone bundle
bun run build

# 4. Start production standalone server
bun start
```

---

## 🐳 Production Docker Build (Bun Runtime)

The production build uses `oven/bun:1-alpine` for ultra-fast multi-stage containerization:

```bash
# Build Docker image locally
docker build -t qos-fe:prod -f ./Dockerfile .

# Run container locally
docker run -p 3000:3000 -e NEXT_PUBLIC_API_URL="http://localhost:8080" qos-fe:prod
```
