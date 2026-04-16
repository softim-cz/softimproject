import type { QueryClient, QueryKey } from "@tanstack/react-query";

export type QueryParams = Record<string, string | number | boolean | null | undefined>;

export function normalizeQueryParams<T extends QueryParams | undefined>(params: T) {
  if (!params) {
    return undefined;
  }

  const normalizedEntries = Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== null && value !== "")
    .sort(([left], [right]) => left.localeCompare(right));

  return normalizedEntries.length > 0 ? Object.fromEntries(normalizedEntries) : undefined;
}

export async function invalidateQueryKeys(queryClient: QueryClient, ...queryKeys: QueryKey[]) {
  await Promise.all(queryKeys.map((queryKey) => queryClient.invalidateQueries({ queryKey })));
}

export const queryKeys = {
  projects: {
    all: () => ["projects"] as const,
    detail: (id: string) => ["projects", id] as const,
    byCodeRoot: () => ["projects", "by-code"] as const,
    byCode: (code: string) => ["projects", "by-code", code] as const,
    users: () => ["projects", "users"] as const,
    customFields: (projectId: string) => ["projects", projectId, "custom-fields"] as const,
  },
  tickets: {
    listRoot: (projectId: string) => ["tickets", projectId] as const,
    list: (projectId: string, params?: QueryParams) =>
      ["tickets", projectId, normalizeQueryParams(params) ?? {}] as const,
    detail: (projectId: string, ticketId: string) => ["tickets", projectId, ticketId] as const,
    byNumber: (projectId: string, number: number) =>
      ["tickets", projectId, "by-number", number] as const,
  },
  kanban: {
    board: (projectId: string) => ["kanban", projectId] as const,
  },
  worklogs: {
    all: () => ["worklogs"] as const,
    list: (params?: QueryParams) => ["worklogs", normalizeQueryParams(params) ?? {}] as const,
  },
  comments: {
    ticket: (projectId: string, ticketId: string) => ["comments", projectId, ticketId] as const,
    project: (projectId: string) => ["project-comments", projectId] as const,
  },
  attachments: {
    ticket: (projectId: string, ticketId: string) => ["attachments", projectId, ticketId] as const,
  },
  github: {
    all: () => ["github"] as const,
    status: () => ["github", "status"] as const,
    repos: () => ["github", "repos"] as const,
  },
};
