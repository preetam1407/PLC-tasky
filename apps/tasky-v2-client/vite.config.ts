import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      "/api/v1": {
        target: "https://localhost:7302",
        changeOrigin: true,
        secure: false
      }
    }
  }
});
