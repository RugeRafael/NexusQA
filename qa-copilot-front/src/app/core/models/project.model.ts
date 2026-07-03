export interface Project {
  id: string;
  name: string;
  description: string;
  status: string;
  startDate: string;
  endDate?: string;
  createdByUserName: string;
  totalAssignedQAs: number;
  createdAt: string;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
  startDate: string;
  endDate?: string;
}

export interface AssignProjectRequest {
  projectId: string;
  userId: string;
  notes?: string;
}