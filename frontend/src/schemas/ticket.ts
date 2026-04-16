import { z } from "zod/v4";

export const createTicketSchema = z.object({
  title: z.string().min(1, "Title is required").max(500),
  description: z.string().max(10000).optional(),
  priority: z.enum(["Low", "Medium", "High", "Critical"]),
  assigneeId: z.string().optional(),
  dueDate: z.string().optional(),
  estimatedHours: z.number().positive().optional(),
});

export type CreateTicketInput = z.infer<typeof createTicketSchema>;

export const updateTicketSchema = z.object({
  title: z.string().min(1, "Title is required").max(500).optional(),
  description: z.string().max(10000).optional(),
  priority: z.enum(["Low", "Medium", "High", "Critical"]).optional(),
  status: z.enum(["Backlog", "Todo", "InProgress", "Review", "Done", "Closed"]).optional(),
  assigneeId: z.string().optional(),
  dueDate: z.string().optional(),
  estimatedHours: z.number().positive().optional(),
});

export type UpdateTicketInput = z.infer<typeof updateTicketSchema>;
