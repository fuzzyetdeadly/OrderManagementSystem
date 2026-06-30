/* 'vitest/config' is a subset of 'vite' */
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true /* Use global test APIs like 'describe', 'it', 'expect', etc. */,
    setupFiles: ["./src/setupTests.ts"] /* Setup before each test suite */,
  },
});
