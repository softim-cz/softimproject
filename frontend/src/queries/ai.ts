import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";

export type AiInvocationTrigger = "AutoSummarize" | "WeeklyReport" | "ManualResummarize" | "Replay";

export interface AiInvocation {
  id: string;
  trigger: AiInvocationTrigger;
  triggeredByDisplayName: string | null;
  model: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  estimatedCostUsd: number;
  outputPreview: string | null;
  success: boolean;
  errorMessage: string | null;
  startedAt: string;
  durationMs: number | null;
  reason: string | null;
}

export interface AiUsageProjectRow {
  projectId: string | null;
  projectName: string | null;
  invocationCount: number;
  totalTokens: number;
  totalCostUsd: number;
  failureCount: number;
}

export interface AiUsage {
  daysWindow: number;
  totalInvocations: number;
  totalTokens: number;
  totalCostUsd: number;
  byProject: AiUsageProjectRow[];
}

export function useTicketAiHistory(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: ["tickets", ticketId, "aiHistory"],
    queryFn: async () => {
      const { data } = await apiClient.get<AiInvocation[]>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/ai/invocations`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useResummarizeTicket(projectId: string, ticketId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (reason: string) => {
      const { data } = await apiClient.post<{ invocationId: string }>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/ai/resummarize`,
        { reason }
      );
      return data;
    },
    onSuccess: () => {
      // Refresh both the AI history list (keyed by ticketId) and the ticket detail
      // (keyed by ["tickets", projectId, "by-number", ...]) so the new summary shows
      // immediately without a hard reload (Ctrl+F5).
      qc.invalidateQueries({ queryKey: ["tickets", ticketId] });
      qc.invalidateQueries({ queryKey: ["tickets", projectId] });
    },
  });
}

export function useAdminAiUsage(days = 30) {
  return useQuery({
    queryKey: ["admin", "aiUsage", days],
    queryFn: async () => {
      const { data } = await apiClient.get<AiUsage>(`/api/v1/admin/ai-usage?days=${days}`);
      return data;
    },
  });
}
