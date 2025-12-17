import { defineConfig } from 'vitest/config';
import path from 'path';

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
});