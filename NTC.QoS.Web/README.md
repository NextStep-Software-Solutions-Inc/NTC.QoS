# NTC QoS & Broadband Mapping Website

This project is a web application for the National Telecommunications Commission (NTC) focused on Quality of Service (QoS) and broadband mapping. The main purpose is to provide users with information about broadband coverage and service quality across different areas.

## Purpose

The application aims to:

- Map broadband coverage areas across the country.
- Display quality of service metrics for various providers.
- Help users identify available broadband services in their location.
- Show which types of broadband (e.g., fiber) are available in a user's area.
- Provide details about providers, available speeds, number of cell sites, and network access points (NAPs) nearby.
- Help users locate providers and infrastructure in their vicinity.

## Basic Features

- **Public Area Coverage Page:** Users can view a map showing broadband coverage in different regions.
- **Open Public Features (No Authentication Required):**
  - View nearby cell sites across the nation.
  - See the number and location of Network Access Points (NAPs) available in any area.
  - Check the maximum and available broadband speeds in a specific area.
  - Identify the main telco players for cell sites in the area.
  - View the generation (e.g., 3G, 4G, 5G) and technology available per location.
- **Service Availability:** See if fiber or other broadband types are available in a specific area.
- **Provider Information:** Show details about available providers, their offered speeds, and locations.
- **QoS Metrics:** Display quality of service statistics for broadband providers.
- **Infrastructure Details:** View the number of cell sites and network access points (NAPs) in the area.
- **User-Friendly Interface:** Simple navigation for both public users and administrators.

## Administrator Features

- **National Administrator:** The national administrator (central office of NTC) has overall access to the entire system and can view all regions and their data nationwide.
- **Regional Administrator:** Regional administrators, managed by the national office, have access and management capabilities for their specific region. They can:
  - View and manage cell sites, NAPs, provinces, municipalities, and barangays within their region.
  - Oversee infrastructure and service data for their jurisdiction.
  - See and manage radio/cellular coverage, including bands, frequencies, technology, and generation (e.g., 3G, 4G, 5G) propagating in their area.
  - Analyze coverage trends and frequency usage within their region.
- **Regulator Dashboard & Analytics:**
  - Dashboard to classify areas as served, underserved, or unserved.
  - View coverage ratios (percentage of area covered by 3G, 4G, 5G, etc.).
  - Analyze speed trends and results in each area over time.
  - Access powerful analytics to monitor broadband and cellular service quality and coverage.

## Technology Stack

This application uses a modern web stack for rapid development and extensibility:

- [Next.js](https://nextjs.org) — React-based framework for building web applications
- [React](https://react.dev) — UI library for building interactive interfaces
- [Tailwind CSS](https://tailwindcss.com) — Utility-first CSS framework
- [ShadCN UI](https://ui.shadcn.com) — Component library for React
- [TanStack Table](https://tanstack.com/table/latest) — Advanced table/grid for React
- [TanStack Query](https://tanstack.com/query/latest) — Data fetching, caching, and deduplication for React
- [Clerk](https://clerk.com) — Authentication and user management
- **C# (.NET 8) Backend Integration** — The application consumes backend APIs written in C# (.NET 8)

The stack is extensible and can integrate more libraries as needed.

## Getting Started

### 1. Install Dependencies

Make sure you have [pnpm](https://pnpm.io/) installed.

```sh
pnpm install
```

### 2. Set Environment Variables

Create a `.env` file in the root directory and add the required environment variables. Example:

```env
# .env
NEXT_PUBLIC_API_URL=https://your-api-url
CLERK_PUBLISHABLE_KEY=your-clerk-publishable-key
CLERK_SECRET_KEY=your-clerk-secret-key
# Add other variables as needed
```

### 3. Run the Development Server

```sh
pnpm dev
```

### 4. Build for Production

```sh
pnpm build
```

### 5. Deploy to Vercel

You can deploy directly to [Vercel](https://vercel.com/) by connecting your repository and following Vercel's deployment instructions.

Alternatively, use the Vercel CLI:

```sh
vercel --prod
```
