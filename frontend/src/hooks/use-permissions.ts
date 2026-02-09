import { useCurrentUser } from "@/queries/auth";
import type { UserPermissions } from "@/types";

const allTrue: UserPermissions = {
  projectsCreate: true,
  projectsRead: true,
  projectsUpdate: true,
  projectsDelete: true,
  timeTrackingCreate: true,
  timeTrackingRead: true,
  timeTrackingUpdate: true,
  timeTrackingDelete: true,
  reportsCreate: true,
  reportsRead: true,
  reportsUpdate: true,
  reportsDelete: true,
};

export function usePermissions(): UserPermissions {
  const { data: user } = useCurrentUser();
  return user?.permissions ?? allTrue;
}
