import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api/v1/tasks": {
        target: "http://localhost:5067",
        changeOrigin: true,
        secure: false
      }
    }
  }
});
