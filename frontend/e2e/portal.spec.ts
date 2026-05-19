import { test, expect } from "./fixtures";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5249";

// Serial mode: the rotation test regenerates the DEMO portal token, which would
// race with other tests that read the current token. Keeping a single worker
// avoids the need to seed a dedicated project per test.
test.describe.configure({ mode: "serial" });

// Helper: read the DEMO project as dev:admin so tests can pick up the *current*
// portal token (it changes in the regeneration test).
async function getDemoProjectAsAdmin(request: import("@playwright/test").APIRequestContext) {
  const res = await request.get(`${apiURL}/api/v1/projects/by-code/DEMO`, {
    headers: { "X-Dev-User-Id": "dev:admin" },
  });
  expect(res.ok()).toBeTruthy();
  return (await res.json()) as { id: string; clientAccessToken: string | null };
}

test.describe("client portal — masking", () => {
  test("backend response does not leak internal ticket fields", async ({ request }) => {
    const project = await getDemoProjectAsAdmin(request);
    expect(project.clientAccessToken).toBeTruthy();

    const res = await request.get(`${apiURL}/api/v1/portal/${project.clientAccessToken}`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();

    const firstColumn = body.board?.columns?.[0];
    expect(firstColumn).toBeDefined();
    const firstTicket = firstColumn.tickets?.[0];
    expect(firstTicket).toBeDefined();

    const ticketKeys = Object.keys(firstTicket);
    expect(ticketKeys).not.toContain("description");
    expect(ticketKeys).not.toContain("estimatedHours");
    expect(ticketKeys).not.toContain("cumulativeWorkedHours");
    expect(ticketKeys).not.toContain("isBillable");
    expect(ticketKeys).not.toContain("invoiced");

    // totalHours must be a scalar aggregate, never a per-user breakdown array.
    expect(typeof body.totalHours).toBe("number");
    expect(Array.isArray(body.totalHours)).toBe(false);

    // Comments field must exist as an empty list contract today — changing this
    // means explicitly re-reviewing internal comment masking.
    expect(Array.isArray(body.comments)).toBe(true);
    expect(body.comments.length).toBe(0);

    // Project payload must not leak infrastructure / credentials.
    const projectKeys = Object.keys(body.project);
    expect(projectKeys).not.toContain("clientAccessToken");
    expect(projectKeys).not.toContain("clientAccessEnabled");
    expect(projectKeys).not.toContain("githubOwner");
    expect(projectKeys).not.toContain("githubInstallationId");
  });

  test("invalid token returns 404", async ({ request }) => {
    const res = await request.get(`${apiURL}/api/v1/portal/this-token-does-not-exist`);
    expect(res.status()).toBe(404);
  });
});

test.describe("client portal — frontend view", () => {
  test("portal URL renders seeded ticket titles anonymously", async ({ request, browser }) => {
    const project = await getDemoProjectAsAdmin(request);

    // Fresh context with no localStorage dev-auth override — portal must work
    // for anonymous clients too.
    const anonContext = await browser.newContext();
    const anonPage = await anonContext.newPage();
    try {
      await anonPage.goto(`/portal/${project.clientAccessToken}`);
      await expect(anonPage.getByText("Wire up demo flow")).toBeVisible();
      await expect(anonPage.getByText("Second seeded ticket")).toBeVisible();
    } finally {
      await anonContext.close();
    }
  });
});

test.describe("client portal — token rotation", () => {
  // Runs last to avoid affecting other tests that rely on the current DEMO token.
  // Ends by leaving the project with a fresh valid token, so re-runs stay green.
  test("regenerate invalidates previous token", async ({ request, page }) => {
    const before = await getDemoProjectAsAdmin(request);
    const oldToken = before.clientAccessToken;
    expect(oldToken).toBeTruthy();

    // Regenerate via API (avoids flakiness of window.confirm dialog + UI path).
    const regen = await request.post(`${apiURL}/api/v1/projects/${before.id}/portal/token`, {
      headers: { "X-Dev-User-Id": "dev:admin" },
    });
    expect(regen.ok()).toBeTruthy();
    const { token: newToken } = (await regen.json()) as { token: string };
    expect(newToken).toBeTruthy();
    expect(newToken).not.toBe(oldToken);

    // Old token rejected.
    const oldRes = await request.get(`${apiURL}/api/v1/portal/${oldToken}`);
    expect(oldRes.status()).toBe(404);

    // New token works.
    const newRes = await request.get(`${apiURL}/api/v1/portal/${newToken}`);
    expect(newRes.ok()).toBeTruthy();

    // Also verify the Settings UI surfaces the regeneration / revoke buttons
    // (smoke — we don't click them, API path covers the behaviour).
    await page.goto("/projects/DEMO/settings");
    await expect(
      page.getByRole("button", { name: /regenerate token|vygenerovat nový token/i })
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /revoke access|zrušit přístup/i })).toBeVisible();
  });
});
