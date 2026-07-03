import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginRequest, AuthResponse, RegisterRequest, RefreshTokenRequest } from '../models/auth.model';
import { ApiResponse } from '../models/api-response.model';
import { jwtDecode } from 'jwt-decode';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;
  private currentUserSubject = new BehaviorSubject<AuthResponse | null>(this.getStoredUser());
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  login(request: LoginRequest): Observable<AuthResponse> {
  return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
    tap(response => {
      this.storeUser(response);
      this.currentUserSubject.next(response);
    })
  );
}

register(request: RegisterRequest): Observable<AuthResponse> {
  return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
    tap(response => {
      this.storeUser(response);
      this.currentUserSubject.next(response);
    })
  );
}



  logout(): void {
    const token = this.getToken();
    if (token) {
      this.http.post(`${this.apiUrl}/logout`, {}).subscribe();
    }
    localStorage.removeItem('qa_copilot_user');
    this.currentUserSubject.next(null);
    this.router.navigate(['/auth/login']);
  }

refreshToken(): Observable<AuthResponse> {
  const user = this.currentUserSubject.value;
  const request: RefreshTokenRequest = { refreshToken: user?.refreshToken || '' };
  return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, request).pipe(
    tap(response => {
      this.storeUser(response);
      this.currentUserSubject.next(response);
    })
  );
}

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const decoded: any = jwtDecode(token);
      return decoded.exp > Date.now() / 1000;
    } catch {
      return false;
    }
  }

  getToken(): string | null {
    return this.currentUserSubject.value?.accessToken || null;
  }

  getCurrentUser(): AuthResponse | null {
    return this.currentUserSubject.value;
  }

  getUserRole(): string {
    return this.currentUserSubject.value?.role || '';
  }

  hasRole(...roles: string[]): boolean {
    return roles.includes(this.getUserRole());
  }

  private storeUser(user: AuthResponse): void {
    localStorage.setItem('qa_copilot_user', JSON.stringify(user));
  }

  private getStoredUser(): AuthResponse | null {
    const stored = localStorage.getItem('qa_copilot_user');
    return stored ? JSON.parse(stored) : null;
  }
}