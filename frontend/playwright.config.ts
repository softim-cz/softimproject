import { defineConfig, devices } from "@playwright/test";

const fePort = process.env.FE_PORT ?? "3000";
const apiPort = process.env.API_PORT ?? "5249";
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? `http://localhost:${fePort}`;
const apiURL = process.env.PLAYWRIGHT_API_URL ?? `http://localhost:${apiPort}`;

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [["list"], ["html", { open: "never" }]] : "list",

  use: {
    baseURL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],

  // In CI the workflow starts BE + FE before invoking playwright, so webServer
  // is skipped to keep control in one place. Locally contributors run
  // `bash scripts/dev-up.sh` in another terminal; Playwright reuses it.
  webServer: process.env.CI
    ? undefined
    : [
        {
          command: "echo 'expecting API on :5249 — start with scripts/dev-up.sh'",
          url: `${apiURL}/health`,
          reuseExistingServer: true,
          timeout: 10_000,
        },
        {
          command: "echo 'expecting FE on :3000 — start with scripts/dev-up.sh'",
          url: baseURL,
          reuseExistingServer: true,
          timeout: 10_000,
        },
      ],
});
