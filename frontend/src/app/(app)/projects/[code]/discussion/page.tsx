"use client";

import { use } from "react";
import { useProjectByCode } from "@/queries/projects";
import { ProjectDiscussion } from "@/components/comments/project-discussion";
import { Skeleton } from "@/components/shared/loading-skeleton";

export default function ProjectDiscussionPage({ params }: { params: Promise<{ code: string }> }) {
  const { code } = use(params);
  const { data: project, isLoading } = useProjectByCode(code);

  if (isLoading || !project) {
    return (
      <div className="max-w-3xl space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-20 w-full" />
      </div>
    );
  }

  return <ProjectDiscussion projectId={project.id} />;
}
