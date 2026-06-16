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

// Calendar day logging (#53). Hard limits per the ticket: at most 16 entries and
// at most 12 hours logged on a single day.
export const MAX_DAY_ITEMS = 16;
export const MAX_DAY_HOURS = 12;

export const dayWorklogItemSchema = z.object({
  projectId: z.string().min(1, "Project is required"),
  ticketId: z.string().min(1, "Ticket is required"),
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z
    .string()
    .min(16, "Description must be at least 16 characters")
    .max(2000, "Description must be at most 2000 characters"),
  isBillable: z.boolean(),
});

export const dayWorklogSchema = z.object({
  items: z.array(dayWorklogItemSchema).min(1, "Add at least one entry").max(MAX_DAY_ITEMS),
});

export type DayWorklogInput = z.infer<typeof dayWorklogSchema>;

// Simplified single-entry form used when the calendar is filtered to one ticket.
export const singleDayWorklogSchema = z.object({
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z
    .string()
    .min(16, "Description must be at least 16 characters")
    .max(2000, "Description must be at most 2000 characters"),
  isBillable: z.boolean(),
});

export type SingleDayWorklogInput = z.infer<typeof singleDayWorklogSchema>;

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
