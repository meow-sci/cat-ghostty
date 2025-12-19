import { defineConfig, type Plugin } from 'vitest/config';
import path from 'path';
import { nodePolyfills } from 'vite-plugin-node-polyfills';

export default defineConfig({
  test: {
    environment: 'jsdom',
    globals: true,
  },
  resolve: {
    alias: {
      '@catty/terminal-emulation': path.resolve(__dirname, '../packages/terminal-emulation/src'),
      '@catty/log': path.resolve(__dirname, '../packages/log/src'),
      '@catty/controller': path.resolve(__dirname, '../packages/controller/src'),
      '@catty/state': path.resolve(__dirname, '../packages/state/src'),
    },
  },
  plugins: [
    // nodePolyfills() as Plugin,
  ],
});