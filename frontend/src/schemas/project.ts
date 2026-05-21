import { z } from "zod/v4";

export const createProjectSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
  code: z
    .string()
    .max(6)
    .regex(/^[A-Z0-9]*$/, "Code must be uppercase alphanumeric")
    .optional()
    .or(z.literal("")),
  description: z.string().max(4000).optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  budgetHours: z.number().positive().optional(),
  budgetAmount: z.number().positive().optional(),
  parentProjectId: z.union([z.string().uuid(), z.literal("")]).optional(),
  projectTemplateId: z.string().uuid({ message: "Šablona projektu je povinná." }),
});

export type CreateProjectInput = z.infer<typeof createProjectSchema>;
