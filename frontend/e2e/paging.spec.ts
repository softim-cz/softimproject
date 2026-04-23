import { test, expect } from "./fixtures";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5249";
const adminHeaders = { "X-Dev-User-Id": "dev:admin" };

async function getDemoProjectId(request: import("@playwright/test").APIRequestContext) {
  const res = await request.get(`${apiURL}/api/v1/projects/by-code/DEMO`, {
    headers: adminHeaders,
  });
  expect(res.ok()).toBeTruthy();
  const { id } = (await res.json()) as { id: string };
  return id;
}

test.describe("paging envelope — list endpoints", () => {
  test("GET /projects returns PagedResult shape", async ({ request }) => {
    const res = await request.get(`${apiURL}/api/v1/projects`, { headers: adminHeaders });
    expect(res.ok()).toBeTruthy();
    const body = await res.json();

    expect(Array.isArray(body.items)).toBe(true);
    expect(typeof body.totalCount).toBe("number");
    expect(typeof body.page).toBe("number");
    expect(typeof body.pageSize).toBe("number");
    expect(typeof body.totalPages).toBe("number");
    expect(typeof body.hasNextPage).toBe("boolean");
    expect(typeof body.hasPreviousPage).toBe("boolean");
  });

  test("GET /worklogs respects pageSize=1", async ({ request }) => {
    const res = await request.get(`${apiURL}/api/v1/worklogs?pageSize=1`, {
      headers: adminHeaders,
    });
    expect(res.ok()).toBeTruthy();
    const body = await res.json();

    expect(Array.isArray(body.items)).toBe(true);
    expect(body.items.length).toBeLessThanOrEqual(1);
    expect(body.pageSize).toBe(1);
  });

  test("GET /comments returns PagedResult and filters by ticket", async ({ request }) => {
    const projectId = await getDemoProjectId(request);

    // Get first ticket id to build comments URL
    const ticketsRes = await request.get(
      `${apiURL}/api/v1/projects/${projectId}/tickets?pageSize=1`,
      { headers: adminHeaders }
    );
    expect(ticketsRes.ok()).toBeTruthy();
    const ticketsBody = await ticketsRes.json();
    const ticketId = ticketsBody.items[0]?.id;
    test.skip(!ticketId, "Demo project has no tickets — skipping comments shape test");

    const res = await request.get(
      `${apiURL}/api/v1/projects/${projectId}/tickets/${ticketId}/comments?pageSize=10`,
      { headers: adminHeaders }
    );
    expect(res.ok()).toBeTruthy();
    const body = await res.json();

    expect(Array.isArray(body.items)).toBe(true);
    expect(typeof body.totalCount).toBe("number");
    expect(body.pageSize).toBe(10);
  });

  test("PageSize is clamped to a sane upper bound", async ({ request }) => {
    // 99999 is well above any configured upper clamp — server should cap it.
    const res = await request.get(`${apiURL}/api/v1/worklogs?pageSize=99999`, {
      headers: adminHeaders,
    });
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.pageSize).toBeLessThanOrEqual(500);
  });
});
