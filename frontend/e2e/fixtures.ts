import { test as base, expect } from "@playwright/test";

export type DevUserId = "dev:admin" | "dev:manager" | "dev:user" | "dev:external";

type DevAuthFixtures = {
  devUser: DevUserId;
};

// Extends the base test with a `devUser` worker-level fixture. Each test sets
// its required dev user via `test.use({ devUser: "dev:manager" })` and the
// fixture injects the right localStorage key before any page script runs, so
// the FE's dev-mode auth hook sees that identity from the first render.
export const test = base.extend<DevAuthFixtures>({
  devUser: ["dev:admin", { option: true }],

  page: async ({ page, devUser }, use) => {
    await page.addInitScript((userId) => {
      window.localStorage.setItem("softim-dev-user-id", userId);
    }, devUser);
    await use(page);
  },
});

export { expect };
