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
│       ├── prod-vps-cicd.yml       <-- Backend (.NET API) CI/CD Workflow
│       └── prod-fe-vps-cicd.yml    <-- Frontend (Next.js Bun) CI/CD Workflow
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

## 🚀 CI/CD & Production Deployment (LightNode VPS)

Deployments are automated via **GitHub Actions** using Environment Scoping (`Production`):

1. **Backend API Workflow (`prod-vps-cicd.yml`):**
   - Triggers on published release or tag (`v*`).
   - Builds `.NET 8` Docker image (`qos-api:prod`).
   - Deploys container **`qos-api`** to server IP `38.54.81.51` on network `ntcqos_db_net`.

2. **Frontend Workflow (`prod-fe-vps-cicd.yml`):**
   - Triggers on published release or tag (`v*`).
   - Builds `Next.js 16` Docker image using Bun (`oven/bun:1-alpine`).
   - Deploys container **`qos-fe`** to server on network `npm_proxy_net`.
