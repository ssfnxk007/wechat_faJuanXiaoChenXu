import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const productionEsbuildOptions = {
  drop: ['console', 'debugger'],
} as const

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
  },
  esbuild: process.env.NODE_ENV === 'production'
    ? (productionEsbuildOptions as never)
    : {},
})
