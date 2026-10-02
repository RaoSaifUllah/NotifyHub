import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: '../tests/e2e',
  use: { baseURL: 'https://127.0.0.1:5173', ignoreHTTPSErrors: true },
  webServer: {
    command: 'npm run dev', url: 'https://127.0.0.1:5173', reuseExistingServer: false,
    ignoreHTTPSErrors: true, env: { NOTIFYHUB_E2E_HTTPS: '1' },
  },
});