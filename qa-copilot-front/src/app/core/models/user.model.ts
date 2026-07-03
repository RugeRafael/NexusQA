export interface User {
  id: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export enum UserRole {
  Admin = 'Admin',
  Senior = 'Senior',
  QAEngineer = 'QAEngineer',
  Viewer = 'Viewer'
}