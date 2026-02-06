import { z } from "zod/v4";

export const createCommentSchema = z.object({
  content: z.string().min(1, "Comment cannot be empty").max(10000),
  isInternal: z.boolean(),
});

export type CreateCommentInput = z.infer<typeof createCommentSchema>;
