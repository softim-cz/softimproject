import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Ticket } from "@/types";
import {
  invalidateQueryKeys,
  normalizeQueryParams,
  queryKeys,
  type QueryParams,
} from "./query-keys";

export interface TicketQueryParams extends QueryParams {
  search?: string;
  taskStateName?: string;
  ticketPriorityName?: string;
  assignee?: string;
  taskTypeName?: string;
  dueDate?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export function useTickets(projectId: string, params?: TicketQueryParams) {
  const normalizedParams = normalizeQueryParams(params);

  return useQuery({
    queryKey: queryKeys.tickets.list(projectId, normalizedParams),
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Ticket>>(
        `/api/v1/projects/${projectId}/tickets`,
        { params: normalizedParams }
      );
      return data;
    },
    enabled: !!projectId,
  });
}

export function useTicket(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: queryKeys.tickets.detail(projectId, ticketId),
    queryFn: async () => {
      const { data } = await apiClient.get<Ticket>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useTicketByNumber(projectId: string, number: number) {
  return useQuery({
    queryKey: queryKeys.tickets.byNumber(projectId, number),
    queryFn: async () => {
      const { data } = await apiClient.get<Ticket>(
        `/api/v1/projects/${projectId}/tickets/by-number/${number}`
      );
      return data;
    },
    enabled: !!projectId && number > 0,
  });
}

export interface CreateTicketPayload {
  projectId: string;
  title: string;
  description?: string;
  ticketPriorityId?: string;
  columnId?: string;
  dueDate?: string;
  estimatedHours?: number;
  taskTypeId?: string;
}

export function useCreateTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, ...ticket }: CreateTicketPayload) => {
      const { data } = await apiClient.post<Ticket>(
        `/api/v1/projects/${projectId}/tickets`,
        ticket
      );
      return data;
    },
    onSuccess: (_, { projectId }) =>
      invalidateQueryKeys(
        queryClient,
        queryKeys.tickets.listRoot(projectId),
        queryKeys.kanban.board(projectId)
      ),
  });
}

export interface UpdateTicketPayload {
  projectId: string;
  id: string;
  ticketId: string;
  title: string;
  description?: string | null;
  ticketPriorityId: string;
  taskStateId: string;
  assigneeId?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  taskTypeId?: string | null;
  parentTicketId?: string | null;
  externalBudget?: number | null;
  externalUser?: string | null;
  externalId?: string | null;
  externalUrl?: string | null;
  externalProject?: string | null;
  implementationNotes?: string | null;
}

export function useUpdateTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...payload }: UpdateTicketPayload) => {
      await apiClient.put(`/api/v1/projects/${payload.projectId}/tickets/${id}`, payload);
    },
    onSuccess: (_, { projectId, id }) =>
      invalidateQueryKeys(
        queryClient,
        queryKeys.tickets.listRoot(projectId),
        queryKeys.tickets.detail(projectId, id),
        queryKeys.kanban.board(projectId)
      ),
  });
}
