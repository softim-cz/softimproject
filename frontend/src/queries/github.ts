import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";

export type PullRequestState = "Open" | "Closed" | "Merged";

export interface LinkedPullRequest {
  id: string;
  provider: string;
  externalId: string;
  url: string;
  title: string;
  branch: string;
  authorLogin: string | null;
  description: string | null;
  commitsCount: number;
  checksStatus: string | null;
  state: PullRequestState;
  openedAt: string;
  closedAt: string | null;
  mergedAt: string | null;
}

export interface CreateBranchResult {
  branchName: string;
  branchUrl: string;
}

export interface LinkedCommit {
  id: string;
  provider: string;
  sha: string;
  message: string;
  url: string;
  authorLogin: string | null;
  committedAt: string;
}

export function useLinkedCommits(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: ["tickets", ticketId, "commits"],
    queryFn: async () => {
      const { data } = await apiClient.get<LinkedCommit[]>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/github/commits`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useLinkedPullRequests(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: ["tickets", ticketId, "pullRequests"],
    queryFn: async () => {
      const { data } = await apiClient.get<LinkedPullRequest[]>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/github/pull-requests`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useCreateTicketBranch(projectId: string, ticketId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const { data } = await apiClient.post<CreateBranchResult>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/github/create-branch`
      );
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["tickets", ticketId] });
    },
  });
}
