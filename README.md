# 🇵🇭 NTC Quality of Service (QoS) Management System (Monorepo)

> **Monorepo Architecture:** Combined ASP.NET 8 Core Web API Backend + Next.js 16 Bun Frontend

---

## 🏗️ System Architecture & Stack

| Layer | Technology / Framework | Environment Details |
| :--- | :--- | :--- |
| **Frontend** | **Next.js 16 + React 19** | **Bun 1.x** Runtime & Package Manager (`NTC.QoS.Web`) |
| **Backend API** | **ASP.NET Core 8 Web API** | Clean Architecture with CQRS & MediatR (`NTC.QoS.API`) |
| **Database** | **PostgreSQL 16 + PostGIS + pgvector** | Extended spatial indexing & AI vector embeddings |
| **Cache** | **Redis 7 (Alpine)** | High-speed memory caching & query acceleration |
| **Blob Storage** | **Azurite (Azure Blob Storage)** | Local S3/Azure Blob file storage emulator |
| **Reverse Proxy** | **Nginx Proxy Manager** | Automated SSL termination & container proxy routing |

---

## 📂 Monorepo Folder Structure

```text
NTC.QoS/                             <-- Monorepo Root
├── .github/
│   └── workflows/
│       ├── development-cicd.yml    <-- Backend (.NET API) Dev VPS CI/CD Workflow
│       ├── prod-vps-cicd.yml       <-- Backend (.NET API) Prod VPS CI/CD Workflow
│       ├── prod-fe-vps-cicd.yml    <-- Frontend (Next.js Bun) Prod VPS CI/CD Workflow
│       └── pr-gemini-reviewer.yml  <-- Automated PR Review Workflow
│
├── NTC.QoS.API/                     <-- Backend API (.NET 8 Core)
├── NTC.QoS.Application/             <-- Business Logic, CQRS Handlers, DTOs
├── NTC.QoS.Domain/                  <-- Entities & Domain Models
├── NTC.QoS.Infrastructure/          <-- EF Core, PostgreSQL Migrations, Repositories
├── NTC.QoS.Tests/                   <-- xUnit Automated Unit Tests
├── NTC.QoS.sln                      <-- Visual Studio Solution File
│
└── NTC.QoS.Web/                    <-- Frontend (Next.js 16 + Bun)
    ├── Dockerfile                   <-- Multi-stage Bun Dockerfile (`oven/bun:1-alpine`)
    ├── package.json
    ├── bun.lock                     <-- Bun Lockfile
    ├── next.config.ts           
    ├── src/
    └── public/
```

---

## 💻 Local Development Setup

### 1. Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Bun 1.x Runtime](https://bun.sh/)
* Docker & Docker Desktop

### 2. Running the Backend API (.NET 8)
```bash
# Navigate to the API folder
cd NTC.QoS.API

# Run API locally
dotnet run
```
API Swagger UI will be available at `http://localhost:5000/swagger`.

### 3. Running the Frontend (Next.js + Bun)
```bash
# Navigate to the Web folder
cd NTC.QoS.Web

# Install dependencies using Bun
bun install

# Run dev server with Turbopack
bun dev
```
Frontend will be live at `http://localhost:3000`.

---

## 🚀 CI/CD & VPS Deployment

Deployments are automated via **GitHub Actions** using GitHub Environment Scoping over SSH / SCP:

### 1. Development Deployment (`Development` Environment)
* **Backend API Workflow (`development-cicd.yml`):**
  - Triggers on push or pull request targeting the `development` branch.
  - Builds & tests .NET 8 solution (`dotnet test`).
  - Packages Docker image `qos-api:dev`.
  - Deploys container **`qos-api-dev`** to the Development VPS over SSH on network `ntcqos_db_net` and connects to `npm_proxy_net`.

### 2. Production Deployment (`Production` Environment)
* **Backend API Workflow (`prod-vps-cicd.yml`):**
  - Triggers on published release or tag (`v*`, `api-v*`).
  - Builds `.NET 8` Docker image (`qos-api:prod`).
  - Deploys container **`qos-api`** to Production VPS on networks `ntcqos_db_net` and `npm_proxy_net`.

* **Frontend Workflow (`prod-fe-vps-cicd.yml`):**
  - Triggers on published release or tag (`v*`, `web-v*`, `fe-v*`).
  - Builds `Next.js 16` Docker image using Bun (`oven/bun:1-alpine`).
  - Deploys container **`qos-fe`** to Production VPS on network `npm_proxy_net`.

