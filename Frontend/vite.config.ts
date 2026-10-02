import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { readFileSync } from 'node:fs';
const testHttps = process.env.NOTIFYHUB_E2E_HTTPS === '1';
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    https: testHttps ? {
      cert: readFileSync(new URL('../TestResults/tls/server-cert.pem', import.meta.url)),
      key: readFileSync(new URL('../TestResults/tls/server-key.pem', import.meta.url)),
    } : undefined,
    proxy: { '/api': 'http://127.0.0.1:5080', '/health': 'http://127.0.0.1:5080', '/ready': 'http://127.0.0.1:5080' },
  },
});