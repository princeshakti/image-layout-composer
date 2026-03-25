import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      // Proxy /api and /outputs requests to the .NET API
      // This means the frontend never makes cross-origin requests,
      // so you don't need CORS enabled on the API during development.
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/outputs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
