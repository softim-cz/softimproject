import { test, expect } from "./fixtures";

// Admin page click-through for #38. The /admin page only loads for GlobalRole.Admin;
// a non-admin sees the "Failed to load users." banner because /api/v1/admin/users 403s.
// Mutation tests touch dev:user (RegularUser, id a0000000-...-0003) so dev:admin remains
// untouched — flipping the only seeded admin would lock the rest of the suite out.

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5249";
const adminHeaders = { "X-Dev-User-Id": "dev:admin" };
const DEV_USER_ID = "a0000000-0000-0000-0000-000000000003";

type Api = import("@playwright/test").APIRequestContext;

async function setGlobalRole(
  request: Api,
  userId: string,
  role: "Admin" | "User",
  caller: string = "dev:admin"
) {
  const res = await request.put(`${apiURL}/api/v1/admin/users/${userId}/global-role`, {
    headers: { "X-Dev-User-Id": caller },
    data: { userId, globalRole: role },
  });
  expect(res.ok()).toBeTruthy();
}

async function setActive(
  request: Api,
  userId: string,
  isActive: boolean,
  caller: string = "dev:admin"
) {
  const res = await request.put(`${apiURL}/api/v1/admin/users/${userId}/active`, {
    headers: { "X-Dev-User-Id": caller },
    data: { userId, isActive },
  });
  expect(res.ok()).toBeTruthy();
}

// Locate the row for dev:user by its unique email cell, then scope further queries to it.
function userRowByEmail(page: import("@playwright/test").Page, email: string) {
  return page.locator("tr", { has: page.getByText(email, { exact: true }) });
}

test.describe("admin page — /admin", () => {
  test.describe.configure({ mode: "serial" });

  test.beforeEach(async ({ request }) => {
    // Every test starts from the known seed state for dev:user so assertions don't
    // depend on whatever the previous test left behind on a retried or reordered run.
    await setGlobalRole(request, DEV_USER_ID, "User");
    await setActive(request, DEV_USER_ID, true);
  });

  test("non-admin sees error banner instead of user table", async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.setItem("softim-dev-user-id", "dev:user");
    });

    await page.goto("/admin");

    await expect(
      page.getByText(/failed to load users|načtení uživatelů se nezdařilo/i)
    ).toBeVisible();
    await expect(page.getByText("admin@softim.cz")).toHaveCount(0);
  });

  test("admin sees user table with self row protected", async ({ page }) => {
    await page.goto("/admin");

    await expect(
      page.getByRole("heading", { name: /user management|správa uživatelů/i })
    ).toBeVisible();
    const adminRow = userRowByEmail(page, "admin@softim.cz");
    await expect(adminRow).toBeVisible();

    // Self-protection: dropdown + active toggle disabled on own row.
    await expect(adminRow.locator("select")).toBeDisabled();
    await expect(
      adminRow.getByRole("button", { name: /active|inactive|aktivní|neaktivní/i })
    ).toBeDisabled();

    // Other users stay editable.
    const userRow = userRowByEmail(page, "user@softim.cz");
    await expect(userRow.locator("select")).toBeEnabled();
  });

  test("admin can promote dev:user to Admin via dropdown", async ({ page, request }) => {
    await page.goto("/admin");
    const userRow = userRowByEmail(page, "user@softim.cz");
    await expect(userRow.locator("select")).toHaveValue("User");

    await userRow.locator("select").selectOption("Admin");

    // React-query refetches /admin/users after the mutation; assert the UI converged.
    await expect(userRow.locator("select")).toHaveValue("Admin", { timeout: 5000 });

    // Verify the API agrees (defensive — select could theoretically stick client-side).
    const res = await request.get(`${apiURL}/api/v1/admin/users`, { headers: adminHeaders });
    const users = (await res.json()) as { id: string; globalRole: string }[];
    expect(users.find((u) => u.id === DEV_USER_ID)?.globalRole).toBe("Admin");
  });

  test("admin can toggle Active status on dev:user", async ({ page, request }) => {
    await page.goto("/admin");
    const userRow = userRowByEmail(page, "user@softim.cz");
    const statusButton = userRow.getByRole("button", {
      name: /active|inactive|aktivní|neaktivní/i,
    });
    await expect(statusButton).toHaveText(/active|aktivní/i);

    await statusButton.click();

    await expect(statusButton).toHaveText(/inactive|neaktivní/i, { timeout: 5000 });

    const res = await request.get(`${apiURL}/api/v1/admin/users`, { headers: adminHeaders });
    const users = (await res.json()) as { id: string; isActive: boolean }[];
    expect(users.find((u) => u.id === DEV_USER_ID)?.isActive).toBe(false);
  });

  test("guardrail error from API renders in the inline row", async ({ page, request }) => {
    // Trigger the last-active-admin guardrail end-to-end: dev:user is promoted to Admin
    // and then deactivated. Logged in through the UI, dev:user still passes the
    // IRequireRole("Admin") check (role claim is GlobalRole, not IsActive) but
    // *doesn't* count toward "other active admins". When dev:user now demotes
    // dev:admin, the backend sees otherAdmins=0 and rejects with 400 — the inline
    // error row renders the message under dev:admin.
    await setGlobalRole(request, DEV_USER_ID, "Admin");
    await setActive(request, DEV_USER_ID, false);

    try {
      await page.addInitScript(() => {
        window.localStorage.setItem("softim-dev-user-id", "dev:user");
      });
      await page.goto("/admin");
      const adminRow = userRowByEmail(page, "admin@softim.cz");
      await expect(adminRow.locator("select")).toHaveValue("Admin");

      await adminRow.locator("select").selectOption("User");

      await expect(page.getByText(/last active admin|poslední aktivní admin/i)).toBeVisible({
        timeout: 5000,
      });
    } finally {
      // Restore the baseline from a session that's still allowed to call the admin API.
      await setActive(request, DEV_USER_ID, true, "dev:admin");
      await setGlobalRole(request, DEV_USER_ID, "User", "dev:admin");
    }
  });
});
