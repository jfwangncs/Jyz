import { fileURLToPath, URL } from 'node:url';

import Vue from '@vitejs/plugin-vue';
import VueJsx from '@vitejs/plugin-vue-jsx';
import { configDefaults, defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [Vue(), VueJsx()],
  resolve: {
    alias: {
      // Allow tests under apps/web-antd to use the "#/..." path alias.
      '#': fileURLToPath(new URL('apps/web-antd/src', import.meta.url)),
    },
  },
  test: {
    environment: 'happy-dom',
    environmentOptions: {
      happyDOM: {
        settings: {
          // happy-dom v20+ disables JS evaluation by default (security fix).
          // Treat disabled script loading as success to preserve test behavior.
          handleDisabledFileLoadingAsSuccess: true,
        },
      },
    },
    exclude: [
      ...configDefaults.exclude,
      '**/e2e/**',
      '**/dist/**',
      '**/.{idea,git,cache,output,temp}/**',
      '**/node_modules/**',
      '**/{stylelint,eslint}.config.*',
      '.prettierrc.mjs',
    ],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      reportsDirectory: './coverage',
      // Only measure pure business logic that is unit-testable without
      // mounting the whole Vue / vxe-grid runtime. UI shell (.vue files)
      // and framework-vendored adapter/request layers are out of scope —
      // they're the Vue equivalent of Migrations/Validators on the dotnet side.
      include: [
        'apps/web-antd/src/api/system/**/*.ts',
        'apps/web-antd/src/views/system/**/data.ts',
      ],
      exclude: ['**/__tests__/**', '**/*.test.ts', '**/index.ts', '**/*.d.ts'],
    },
  },
});
