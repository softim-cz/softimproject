import { test, expect } from "./fixtures";

test.describe("dev-stack smoke", () => {
  test("root redirects to /dashboard without login prompt", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByRole("heading", { level: 1, name: /dashboard|přehled/i })).toBeVisible();
  });

  test("dashboard shows seeded Demo Project card", async ({ page }) => {
    await page.goto("/dashboard");
    const demo = page.getByRole("heading", { name: "Demo Project", level: 3 });
    await expect(demo).toBeVisible();
    await expect(page.getByText("DEMO", { exact: true })).toBeVisible();
  });

  test("projects list shows Demo Project", async ({ page }) => {
    await page.goto("/projects");
    await expect(page.getByRole("heading", { name: "Demo Project" })).toBeVisible();
  });

  test("project task list shows both seeded tickets", async ({ page }) => {
    await page.goto("/projects/DEMO/tasks");
    await expect(page.getByText("Wire up demo flow")).toBeVisible();
    await expect(page.getByText("Second seeded ticket")).toBeVisible();
  });

  test.describe("as dev:external (Guest)", () => {
    test.use({ devUser: "dev:external" });

    test("still reaches dashboard without login", async ({ page }) => {
      await page.goto("/dashboard");
      await expect(page).toHaveURL(/\/dashboard$/);
    });

    test("can read Demo Project task list (Guest has read access)", async ({ page }) => {
      await page.goto("/projects/DEMO/tasks");
      await expect(page.getByText("Wire up demo flow")).toBeVisible();
    });
  });
});
