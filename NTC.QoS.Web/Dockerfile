FROM oven/bun:1-alpine AS base

# 1. Install dependencies with Bun
FROM base AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

COPY package.json bun.lock* pnpm-lock.yaml* yarn.lock* package-lock.json* ./
RUN bun install --frozen-lockfile || bun install

# 2. Build Next.js application using Bun
FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

ENV NEXT_TELEMETRY_DISABLED 1
ENV NODE_ENV production
ENV NEXT_IGNORE_ESLINT 1

# Provide build-time fallback for Clerk publishable key during static prerendering
ARG NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY="pk_test_placeholder_key_for_static_prerendering"
ENV NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=$NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY

ARG NEXT_PUBLIC_API_URL="http://localhost:8080"
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

RUN bun run build

# 3. Production runner stage using Bun
FROM base AS runner
WORKDIR /app

ENV NODE_ENV production
ENV NEXT_TELEMETRY_DISABLED 1

RUN addgroup --system --gid 1001 bunjs
RUN adduser --system --uid 1001 bunuser

COPY --from=builder /app/public ./public

RUN mkdir .next
RUN chown bunuser:bunjs .next

COPY --from=builder --chown=bunuser:bunjs /app/.next/standalone ./
COPY --from=builder --chown=bunuser:bunjs /app/.next/static ./.next/static

USER bunuser

EXPOSE 3000

ENV PORT 3000
ENV HOSTNAME "0.0.0.0"

CMD ["bun", "run", "server.js"]
