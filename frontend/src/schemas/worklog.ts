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

export const updateWorklogSchema = z.object({
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
