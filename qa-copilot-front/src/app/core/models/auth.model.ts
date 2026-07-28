export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userName: string;
  role: string;
  dashboardType: string; // 'admin' | 'senior' | 'qa'
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
