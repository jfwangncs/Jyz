# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## Tech Stack

1. Vue 3 + TypeScript + Vite monorepo project based on **pnpm workspaces** + **Turborepo**.
2. Based on the **Vben Admin** template; only the **Ant Design Vue** variant is kept (alternative UI variants in upstream Vben were removed). Core UI is built on TailwindCSS + shadcn-vue.
3. Requires Node ≥ 20.19.0 and pnpm ≥ 10.
4. Uses **prettier** + **eslint** + **stylelint** for code linting and formatting.
5. Uses **vitest** for unit testing.
6. Uses **commitlint** for commit message conventions.
7. Uses **czg** for interactive commit prompts.
8. Uses **lefthook** for Git hooks.
9. Uses **vsh** for code linting and formatting utilities.
10. Uses **turbo** for building.
11. Uses **vite** for development.
12. Uses **vue-tsc** for type checking.

```bash
# Additional checks
pnpm check:circular # Circular dependency scan
pnpm check:dep      # depcheck dependency audit
pnpm check:cspell   # Spell check

# Cleanup
pnpm clean          # Remove dist, node_modules, and other build artifacts
pnpm reinstall      # clean + reinstall

# Interactive conventional commit
pnpm commit         # czg commit wizard
```

Turbo tasks cascade via `dependsOn: ["^build"]`, so building an app automatically builds all its dependency packages first.

## Monorepo Directory Structure

```text
apps/
  web-antd/         # Ant Design Vue application

packages/
  @core/            # Framework core (no dependency on specific UI libraries)
    base/           # Shared utilities, caching, color handling, type definitions
    composables/    # Core Vue composables
    preferences/    # PreferenceManager class (reactive, persistent configuration)
    ui-kit/         # UI component fragments: form-ui, layout-ui, menu-ui, popup-ui, shadcn-ui, tabs-ui
  effects/          # Higher-level modules that may depend on @core and UI libraries
    access/         # Route/menu generation and permission directives
    common-ui/      # Common UI components (ApiComponent, IconPicker, VCropper, Tippy, etc.)
    hooks/          # useAppConfig and others
    layouts/        # BasicLayout, login page, various widgets
    plugins/        # Motion and other plugins
    request/        # RequestClient (axios wrapper + interceptor system)
  constants/        # Global constants (LOGIN_PATH, etc.)
  icons/            # Iconify icon wrappers
  locales/          # vue-i18n initialization, loadLocalesMap utility
  preferences/      # Public API exposing @core/preferences
  stores/           # Pinia global stores: useAccessStore, useUserStore, useTabbarStore
  styles/           # Global CSS / TailwindCSS base styles
  types/            # Shared TypeScript types
  utils/            # Shared utility functions (mergeRouteModules, mapTree, etc.)

internal/
  lint-configs/     # ESLint, Prettier, Stylelint, commitlint config packages
  node-utils/       # Build-time Node utilities
  tailwind-config/  # Shared Tailwind configuration
  tsconfig/         # Base tsconfig
  vite-config/      # Shared Vite config factory + plugin collection

scripts/
  vsh/              # CLI tools (lint, check-dep, check-circular, publint)
  turbo-run/        # Interactive turbo runner
```

## Core Architecture

### Application Bootstrap Flow

Each app's `src/main.ts` calls `bootstrap(namespace)` (defined in `src/bootstrap.ts`), which executes in order:

1. Initializes the **component adapter** (`src/adapter/component/index.ts`) — maps generic form component names to concrete UI library components.
2. Calls `initSetupVbenForm()` (`src/adapter/form.ts`) to configure the generic form system.
3. Sequentially initializes i18n, Pinia stores, permission directives, Tippy, router, and MotionPlugin, then mounts to `#app`.

### Preferences System

`@vben/preferences` exports a singleton `preferences` (`PreferenceManager`). It is reactive, automatically persisted to localStorage (prefixed with the app namespace), and drives theme CSS variable updates. Each app calls `defineOverridesPreferences()` in `src/preferences.ts` to override defaults without modifying core code.

### Permission / Access System

`@vben/access` (`packages/effects/access`) supports three access modes:

- **frontend**: Filters static routes based on user roles.
- **backend**: Fetches menus from an API (`getAllMenusApi`) and dynamically registers routes.
- **mixed**: Uses both approaches simultaneously.

The route guard (`src/router/guard.ts`) calls `generateAccess()` on the first post-login navigation, stores the result in `useAccessStore`, then redirects to the target page. The `v-access` directive and `<AccessControl>` component control UI element visibility by permission code or role.

### Request Client

`@vben/request` wraps Axios as `RequestClient`. Each app creates its own instance in `src/api/request.ts` with the following interceptors:

- **Request interceptor**: Automatically attaches Bearer Token and Accept-Language headers.
- **`defaultResponseInterceptor`**: Unwraps the `{ code, data, message }` response format.
- **`authenticateResponseInterceptor`**: Handles 401 responses by auto-refreshing the token or redirecting to login.
- **`errorMessageResponseInterceptor`**: Calls `message.error()` to display error messages.

In API files, import `requestClient` (auto-unwraps response) or `baseRequestClient` (raw response) from `#/api/request`.

### Route Organization

- `src/router/routes/modules/*.ts`: Dynamic routes requiring authentication.
- `src/router/routes/core/`: Always-accessible routes (login page, 404, etc.).
- `mergeRouteModules(import.meta.glob(...))` aggregates route module files.
- Dynamic routes are registered at runtime by the permission system.

### Adapter Pattern

Each UI library app provides adapters under `src/adapter/` to bridge `@vben/common-ui` generic form/modal/drawer components to the specific component library. This is the primary difference between `web-antd`, `web-ele`, and other apps.

### Global Pinia Stores

- `useAccessStore`: Token, routes, menus, lock screen, and login expiry state.
- `useUserStore`: User info, roles, homePath.
- `useTabbarStore`: Open tabs management.

All stores are initialized together via `initStores(app, { namespace })` from `@vben/stores`.

## Development Conventions

- **Path alias**: `#/*` points to each app's `./src/*` (defined in `package.json#imports`).
- **Dependency version management**: Internal packages use `workspace:*`; third-party packages use `catalog:` (versions centrally managed in `pnpm-workspace.yaml#catalog`).
- **Commit conventions**: Follow Conventional Commits (`feat`, `fix`, `chore`, `docs`, `refactor`, `perf`, `test`, `ci`, `style`, `types`, `revert`), enforced by lefthook + commitlint.
- **Pre-commit hook** (lefthook): Automatically runs prettier + eslint + stylelint on staged files. Use `pnpm commit` (czg) for guided commits.
- **Adding new pages**: Create a `.vue` file under `src/views/`, add a route module under `src/router/routes/modules/`; if using backend mode, also ensure the backend API returns the corresponding menu data.
- **Internationalization**: Use `$t('key')` throughout; locale files are in `packages/locales/`, and app-level i18n files are in `src/locales/langs`.
- **Error messages come from the backend**: The .NET API returns `message` already localized by `Accept-Language` (see `src/api/Jyz/Resources/EnumMessages.*.resx`). Do **not** create frontend `error.json` files or map `errorCode` to local strings — `request.ts` shows `response.data.message` directly.

## Modification Boundaries (IMPORTANT)

This project is based on the **Vben Admin** framework, which ships as a Monorepo template (clone-and-modify, not an npm package). To keep future framework upgrades cheap, **business code MUST stay inside `apps/web-antd/src/`**. Treat every other directory as upstream / read-only.

### Allowed (write freely)

- `apps/web-antd/src/views/**` — business pages
- `apps/web-antd/src/api/**` — API clients for our backend
- `apps/web-antd/src/router/routes/modules/**` — business route modules
- `apps/web-antd/src/locales/langs/**` — business i18n strings
- `apps/web-antd/src/store/**` — app-scoped Pinia stores (NOT the global ones in `packages/stores`)
- `apps/web-antd/src/adapter/**` — the **only** sanctioned place to extend / override framework defaults (form components, vxe-table presets, etc.)

### Read-only (do not modify)

- `packages/**` — Vben framework code (`@core`, `effects`, `stores`, `preferences`, `locales`, `icons`, `utils`, `types`, `constants`, `styles`)
- `internal/**` — build / lint / tsconfig / vite-config tooling
- `scripts/**` — CLI tools (`vsh`, `turbo-run`)
- `apps/web-antd/src/{main.ts, bootstrap.ts, preferences.ts, layouts/**}` — framework wiring; only edit if absolutely required and document why

### When you need behavior the framework doesn't expose

1. Try the adapter layer first (`apps/web-antd/src/adapter/`).
2. Wrap the framework component in a thin business component under `apps/web-antd/src/components/`.
3. Add a Pinia store / composable under `apps/web-antd/src/`.
4. Only as a last resort modify `packages/**` — and record the file + reason in the upgrade log below so it can be re-applied after a Vben upgrade.

### Upgrade workflow

We do **not** track Vben as a git remote (the upstream repo's root layout differs from ours, where Vue lives under `src/web/`). Upgrade by manual cherry-pick:

1. Watch [vue-vben-admin releases](https://github.com/vbenjs/vue-vben-admin/releases). Skim release notes for security / bug-fix commits worth taking.
2. For each commit you want, fetch its patch:
   ```powershell
   curl -L https://github.com/vbenjs/vue-vben-admin/commit/<sha>.patch -o vben.patch
   git apply --directory=src/web/ --3way vben.patch
   ```
3. Resolve any conflicts. They should only land inside `packages/**` or `internal/**` if the rules above were followed.
4. Run `pnpm install`, `pnpm lint`, `pnpm test:coverage`, `pnpm -F @vben/web-antd build` to verify.
5. Commit with message `chore(vben): cherry-pick <sha> — <subject>` so future maintainers can audit applied upstream changes.

If `git apply` rejects too many hunks, manually open the upstream files on GitHub and copy the changes file-by-file — easier than fighting the patch tool.

### Removed framework pieces

The following were dropped from the upstream Vben repo because we don't use them. **Do not re-introduce them when upgrading**:

- `apps/web-ele`, `apps/web-naive`, `apps/web-tdesign`, `apps/web-antdv-next` — alternative UI library variants
- `apps/playground` — component playground
- `apps/docs` — VitePress documentation site
- `apps/backend-mock` — Nitro mock server (we use the real .NET backend)
