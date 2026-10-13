import { defineConfig } from '@vben/vite-config';

export default defineConfig(async () => {
  return {
    application: {},
    vite: {
      server: {
        proxy: {
          '/api': {
            changeOrigin: true,
            secure: false,
            target: 'https://localhost:5000',
            ws: true,
          },
        },
      },
    },
  };
});
