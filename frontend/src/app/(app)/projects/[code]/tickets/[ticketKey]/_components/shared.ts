import type { Ticket, UserOption } from "@/types";

export type TicketPatch = Partial<
  Pick<
    Ticket,
    | "title"
    | "description"
    | "ticketPriorityId"
    | "taskStateId"
    | "assigneeId"
    | "dueDate"
    | "estimatedHours"
    | "taskTypeId"
    | "parentTicketId"
    | "externalBudget"
    | "externalUser"
    | "externalId"
    | "externalUrl"
    | "externalProject"
    | "implementationNotes"
  >
>;

export function buildUpdatePayload(ticket: Ticket, patch: TicketPatch) {
  // Backend UpdateTicketCommand requires the full record; merge current ticket values with the patch.
  return {
    projectId: ticket.projectId,
    id: ticket.id,
    ticketId: ticket.id,
    title: patch.title ?? ticket.title,
    description: patch.description ?? ticket.description ?? null,
    ticketPriorityId: patch.ticketPriorityId ?? ticket.ticketPriorityId,
    taskStateId: patch.taskStateId ?? ticket.taskStateId,
    assigneeId: "assigneeId" in patch ? patch.assigneeId : ticket.assigneeId,
    dueDate: "dueDate" in patch ? patch.dueDate : ticket.dueDate,
    estimatedHours: "estimatedHours" in patch ? patch.estimatedHours : ticket.estimatedHours,
    taskTypeId: "taskTypeId" in patch ? patch.taskTypeId : ticket.taskTypeId,
    parentTicketId: "parentTicketId" in patch ? patch.parentTicketId : ticket.parentTicketId,
    externalBudget: "externalBudget" in patch ? patch.externalBudget : ticket.externalBudget,
    externalUser: "externalUser" in patch ? patch.externalUser : ticket.externalUser,
    externalId: "externalId" in patch ? patch.externalId : ticket.externalId,
    externalUrl: "externalUrl" in patch ? patch.externalUrl : ticket.externalUrl,
    externalProject: "externalProject" in patch ? patch.externalProject : ticket.externalProject,
    implementationNotes:
      "implementationNotes" in patch ? patch.implementationNotes : ticket.implementationNotes,
  };
}

export function localizedLookupName(
  locale: string,
  name: string,
  nameCs?: string | null,
  nameEn?: string | null
): string {
  if (locale.startsWith("cs")) return nameCs?.trim() || name;
  return nameEn?.trim() || name;
}

export function userPickerLabel(u: UserOption) {
  return u.displayName?.trim() ? u.displayName : u.email;
}

// Maps a GitHub checks/CI status to a colored badge (success → green, failure/error →
// red, pending/queued/in_progress → amber, everything else neutral).
export function checksBadgeClass(status: string): string {
  const s = status.toLowerCase();
  if (s === "success")
    return "bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300";
  if (s === "failure" || s === "error" || s === "timed_out" || s === "cancelled")
    return "bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300";
  if (s === "pending" || s === "queued" || s === "in_progress" || s === "action_required")
    return "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300";
  return "bg-muted text-muted-foreground";
}
