export interface DashboardMetrics {
  totalDocuments: number;
  totalTestCasesGenerated: number;
  averageConfidenceScore: number;
  totalUsers: number;
  activityByModule: ModuleActivity[];
}

export interface ModuleActivity {
  module: string;
  totalActions: number;
  successCount: number;
  failureCount: number;
}

export interface UserActivity {
  userId: string;
  userName: string;
  email: string;
  totalDocuments: number;
  totalTestCasesGenerated: number;
  totalTestPlansAnalyzed: number;
  totalSessionSeconds: number;
  lastActivityAt?: string;
  assignedProjects: string[];
}