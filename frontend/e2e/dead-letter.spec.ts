import { test, expect } from "./fixtures";

// Admin dead-letter section (#13) — the exhaustive "enqueue → list → replay/dismiss"
// cycle is covered by the backend integration tests (DeadLetterTests), because E2E
// can't easily populate DLQ rows without a test-only seed endpoint. Here we just
// verify the UI wires up cleanly and the endpoints are properly gated.

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5249";

test.describe("admin page — dead-letter queue", () => {
  test("admin sees the DLQ section and its list area (empty or populated)", async ({ page }) => {
    await page.goto("/admin/users");
    await expect(
      page.getByRole("heading", {
        name: /dead-letter queue|dead-letter fronta/i,
        level: 2,
      })
    ).toBeVisible();

    const section = page.locator("section", {
      has: page.getByRole("heading", {
        name: /dead-letter queue|dead-letter fronta/i,
      }),
    });
    const emptyTitle = section.getByText(
      /no pending failures|no dead-letter entries|žádné nevyřízené chyby|žádné dead-letter záznamy/i
    );
    const table = section.locator("table");
    await expect(emptyTitle.or(table)).toBeVisible();
  });

  test("replay endpoint 404s for an unknown id (sanity check that routing + auth wire up)", async ({
    request,
  }) => {
    const response = await request.post(
      `${apiURL}/api/v1/admin/dead-letter/00000000-0000-0000-0000-000000000000/replay`,
      { headers: { "X-Dev-User-Id": "dev:admin" } }
    );
    expect(response.status()).toBe(404);
  });

  test("non-admin cannot reach the DLQ list endpoint", async ({ request }) => {
    const response = await request.get(`${apiURL}/api/v1/admin/dead-letter`, {
      headers: { "X-Dev-User-Id": "dev:user" },
    });
    expect(response.status()).toBe(403);
  });
});
