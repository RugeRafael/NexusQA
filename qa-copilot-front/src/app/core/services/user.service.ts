import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/api/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl).pipe(
      map(r => r.data || r)
    );
  }

  getQAEngineers(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl).pipe(
      map(r => {
        const users = r.data || r;
        return Array.isArray(users)
          ? users.filter((u: any) => u.role === 'QAEngineer' || u.role === 'Senior')
          : [];
      })
    );
  }
}