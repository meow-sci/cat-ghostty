import { defineConfig, type Plugin } from 'vite';
import { nodePolyfills } from 'vite-plugin-node-polyfills';

export default defineConfig({
  build: {
    lib: {
      entry: './src/index.ts',
      name: 'index',
      fileName: 'index',
      formats: ["es"],
    },
  },
  plugins: [
    nodePolyfills() as Plugin,
  ],
});
