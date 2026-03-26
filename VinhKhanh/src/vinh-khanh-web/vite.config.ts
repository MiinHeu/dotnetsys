import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

// https://vite.dev/config/
const __dirname = path.dirname(fileURLToPath(import.meta.url))

export default defineConfig({
  plugins: [react()],
  css: {
    postcss: './postcss.config.js',
  },
	resolve: {
		alias: {
			'@': path.resolve(__dirname, './src'),
		},
	},
	server: {
		port: 5173,
		host: '127.0.0.1',
		proxy: {
			'/api': {
				target: 'http://localhost:5283',
				changeOrigin: true,
			},
			'/hubs': {
				target: 'http://localhost:5283',
				changeOrigin: true,
				ws: true,
			},
		},
	},
})
