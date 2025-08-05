import { defineConfig, loadEnv } from 'vite'
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), "");

    return {
        plugins: [plugin()],
        server: {
            port: parseInt(env.VITE_PORT)
        },
        build: {
            outDir: "dist",
            rollupOptions: {
                input: "./index.html"
            }
        }
    }
})