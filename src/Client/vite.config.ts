// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import fable from "vite-plugin-fable";
import { qrcode } from "vite-plugin-qrcode";

export default defineConfig(() => {
  const useMock = process.env.VITE_USE_MOCK_API === 'true';

  return {
    plugins: [
      fable({
        fsproj: "src/src.fsproj",
        failOnFirstError: true,
      }),
      react(),
      qrcode(), // <-- prints a QR for the Network URL on dev start
    ],
    server: {
      host: true, // so LAN devices can access
      proxy: useMock ? undefined : {
        '/api': {
          target: 'http://localhost:5199',
          changeOrigin: true,
          secure: false,
        }
      }
    }
  }
});