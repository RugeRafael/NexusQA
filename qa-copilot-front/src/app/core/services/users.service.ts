import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly apiUrl = environment.apiUrl + '/api/users';

  constructor(private http: HttpClient) {}

  getAll(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl).pipe(map(r => r.data || r));
  }

  create(user: { fullName: string; email: string; password: string; role: string }): Observable<any> {
    return this.http.post<any>(this.apiUrl, user).pipe(map(r => r.data || r));
  }

  updateRole(id: string, role: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/role`, { role }).pipe(map(r => r.data || r));
  }

  toggleActive(id: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/toggle-active`, {}).pipe(map(r => r.data || r));
  }

  resetPassword(id: string, newPassword: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/reset-password`, { newPassword }).pipe(map(r => r.data || r));
  }
}
