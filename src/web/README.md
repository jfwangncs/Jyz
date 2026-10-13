# Vue Frontend

A Vue 3 + TypeScript + Vite monorepo frontend, based on [Vben Admin](https://github.com/vbenjs/vue-vben-admin) v5, paired with the [JfYu.WebApi.Template](../dotnet/content/README.md) backend.

## Tech Stack

- **Vue 3.5** 鈥?Composition API + `<script setup>`
- **TypeScript 5** 鈥?strict mode
- **Vite 7** 鈥?build tooling
- **pnpm** + **Turborepo** 鈥?monorepo orchestration
- **Ant Design Vue 4** + **VxeTable 4** 鈥?UI components and data grid
- **Pinia** 鈥?state management
- **vue-i18n** 鈥?internationalization (zh-CN / en-US)
- **Vitest 4** + `@vitest/coverage-v8` 鈥?unit tests with coverage

## Repository Layout

```text
apps/
  web-antd/                # Main Ant Design Vue application
    src/
      api/                 # API request modules (system/*, core/*)
      views/               # Pages
        system/            # Business pages (user, role, permission, dict, ...)
      adapter/             # Vben framework adapters (read-only, vendored)
      router/              # Routes (vendored, do not modify)
      locales/             # i18n entrypoints (vendored)
packages/                  # Shared library packages (@core, effects, stores, utils)
internal/                  # Internal tooling 鈥?eslint/prettier/tsconfig/vite-config
scripts/                   # Build & deploy scripts
```

## Modification Boundaries (IMPORTANT)

To stay compatible with upstream Vben Admin updates, only the following paths are owned by this template and freely editable:

| Path | Status |
| --- | --- |
| `apps/web-antd/src/api/system/**` | **Editable** 鈥?business APIs |
| `apps/web-antd/src/views/system/**` | **Editable** 鈥?business pages |
| `apps/web-antd/src/locales/**/system.json` etc. | **Editable** 鈥?i18n keys for business |
| `apps/web-antd/src/adapter/**` | Read-only 鈥?Vben adapter |
| `apps/web-antd/src/router/**` | Read-only 鈥?Vben router |
| `apps/web-antd/src/views/_core/**` | Read-only 鈥?Vben auth/error pages |
| `apps/web-antd/src/views/dashboard/**` | Read-only 鈥?Vben sample dashboards |
| `packages/**`, `internal/**`, `scripts/**` | Read-only 鈥?vendored Vben |

When upgrading Vben, cherry-pick changes into the read-only paths and keep `apps/web-antd/src/{api,views}/system/**` untouched. See `AGENTS.md` for full conventions.

## Prerequisites

- Node.js 鈮?22
- pnpm 鈮?9 (`corepack enable` then `corepack prepare pnpm@latest --activate`)

## Commands

All commands run from `src/web/`.

```bash
# Install dependencies
pnpm install

# Start the Ant Design Vue app (default: http://localhost:5666)
pnpm --filter @vben/web-antd dev

# Build for production
pnpm --filter @vben/web-antd build

# Lint everything (eslint + prettier + stylelint + commitlint)
pnpm lint

# Auto-fix lint & formatting
pnpm format

# Run unit tests
pnpm test:unit

# Run unit tests with coverage report (lcov + text)
pnpm test:coverage

# Type-check
pnpm check:type

# Clean build artifacts and node_modules
pnpm clean
```

## Coverage Scope

Vitest is configured (in [vitest.config.ts](vitest.config.ts)) to instrument:

- `apps/web-antd/src/api/system/**/*.ts`
- `apps/web-antd/src/views/system/**/data.ts`

These represent the business surface that ships with this template. The `.vue` UI shells and Vben-vendored code are excluded 鈥?they are smoke-tested at the integration layer.

The CI gate requires **鈮?90 % line coverage** on the instrumented files (currently 100 %).

## Build & Release

- **Build output**: `pnpm --filter @vben/web-antd build` 鈫?`apps/web-antd/dist`.
- **Docker**: root `Dockerfile` (Node build 鈫?nginx), container listens on `8080`.
- **Release**: versions follow SemVer and are driven by git tags 鈥?`git tag 1.2.3 && git push origin 1.2.3` triggers `.github/workflows/docker-push.yml`, pushing `jfyu-web:<tag>` + `:latest`.
- **API base URL**: set `VITE_GLOB_API_URL` in `apps/web-antd/.env.production` before building.

## Backend Integration

The frontend talks to the backend defined in [`../dotnet/`](../dotnet/content/README.md). Configure the API base URL via environment variables in `apps/web-antd/.env.development` / `.env.production`.

## Conventions

- Path alias: `#/*` 鈫?`apps/web-antd/src/*`
- All code comments and identifiers are in **English**; user-facing strings live in i18n JSON files.
- Add new business modules under `apps/web-antd/src/{api,views}/system/<module>/`. See `AGENTS.md` and [`docs/fullstack-feature-guide.md`](../../docs/fullstack-feature-guide.md) (Claude Code users: the `fullstack-feature` skill wraps the same guide) for the standard scaffolding.
- Tests live in `__tests__/` next to the file under test (e.g. `views/system/user/__tests__/data.test.ts`).
- Never edit files in `internal/`, `packages/`, `scripts/`, `apps/web-antd/src/router/`, `apps/web-antd/src/adapter/`, or `apps/web-antd/src/views/{_core,dashboard}/`.

## License

MIT
