# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM node:22-slim AS build

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
ENV NODE_OPTIONS=--max-old-space-size=8192
ENV TZ=Asia/Shanghai

# Enable corepack so it picks up the pinned pnpm version from package.json
RUN corepack enable

WORKDIR /app

# Copy the whole pnpm workspace (all packages + lockfile are required)
COPY . .

# Optional: override the backend API base URL at build time.
#   docker build --build-arg VITE_GLOB_API_URL=https://api.example.com -t web-antd .
ARG VITE_GLOB_API_URL
RUN if [ -n "$VITE_GLOB_API_URL" ]; then \
      printf 'VITE_GLOB_API_URL=%s\n' "$VITE_GLOB_API_URL" > .env.production.local; \
    fi

# Install dependencies from the lockfile (reproducible builds)
RUN --mount=type=cache,id=pnpm-store,target=/pnpm/store \
    pnpm install --frozen-lockfile

# Build the Ant Design Vue app (turbo builds its workspace dependencies first)
RUN pnpm run build:antd

# ---------- Runtime stage ----------
FROM nginx:stable-alpine AS runtime

# Serve JS modules with the correct MIME type and drop the default site config
RUN echo "types { application/javascript js mjs; }" > /etc/nginx/conf.d/mjs.conf \
    && rm -rf /etc/nginx/conf.d/default.conf

# Copy the built SPA and the nginx server config (listens on 8080)
COPY --from=build /app/apps/web-antd/dist /usr/share/nginx/html
COPY scripts/deploy/nginx.conf /etc/nginx/nginx.conf

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
