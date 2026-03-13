// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { qrcode } from "vite-plugin-qrcode";

export default defineConfig(() => {
  return {
    plugins: [
      react(),
      qrcode(), // <-- prints a QR for the Network URL on dev start
    ],
    server: {
      host: true, // so LAN devices can access
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true,
          secure: false,
        }
      }
    }
  }
});
