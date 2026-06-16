import { z } from "zod/v4";

export const createWorklogSchema = z.object({
  projectId: z.string().min(1, "Project is required"),
  ticketId: z.string().min(1, "Ticket is required"),
  date: z.string().min(1, "Date is required"),
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z
    .string()
    .min(16, "Description must be at least 16 characters")
    .max(2000, "Description must be at most 2000 characters"),
  isBillable: z.boolean(),
  overrideUserId: z.string().optional(),
});

export type CreateWorklogInput = z.infer<typeof createWorklogSchema>;

export const worklogBatchItemSchema = z.object({
  date: z.string().min(1, "Date is required"),
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z
    .string()
    .min(16, "Description must be at least 16 characters")
    .max(2000, "Description must be at most 2000 characters"),
  isBillable: z.boolean(),
});

export const worklogBatchSchema = z.object({
  items: z.array(worklogBatchItemSchema).min(1, "Add at least one entry").max(50, "Max 50 entries"),
});

export type WorklogBatchInput = z.infer<typeof worklogBatchSchema>;
export type WorklogBatchItemInput = z.infer<typeof worklogBatchItemSchema>;

export const updateWorklogSchema = z.object({
  ticketId: z.string().min(1, "Ticket is required"),
  date: z.string().min(1, "Date is required"),
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z
    .string()
    .min(16, "Description must be at least 16 characters")
    .max(2000, "Description must be at most 2000 characters"),
  isBillable: z.boolean(),
  invoiced: z.string().max(200).optional(),
  overrideUserId: z.string().optional(),
});

export type UpdateWorklogInput = z.infer<typeof updateWorklogSchema>;
