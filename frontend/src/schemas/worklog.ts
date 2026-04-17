import { z } from "zod/v4";

export const createWorklogSchema = z.object({
  projectId: z.string().min(1, "Project is required"),
  ticketId: z.string().optional(),
  date: z.string().min(1, "Date is required"),
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z.string().max(2000).optional(),
  isBillable: z.boolean(),
});

export type CreateWorklogInput = z.infer<typeof createWorklogSchema>;

export const updateWorklogSchema = z.object({
  date: z.string().min(1, "Date is required"),
  hours: z.number().positive("Hours must be positive").max(24, "Max 24 hours"),
  description: z.string().max(2000).optional(),
  isBillable: z.boolean(),
  invoiced: z.string().max(200).optional(),
});

export type UpdateWorklogInput = z.infer<typeof updateWorklogSchema>;
