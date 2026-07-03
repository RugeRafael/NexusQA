import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly apiUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  getUsersActivity(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/metrics/dashboard`).pipe(
      map(r => {
        const data = r.data || r;
        return data.activityByModule || [];
      })
    );
  }

  getDashboard(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/metrics/dashboard`).pipe(
      map(r => r.data || r)
    );
  }
}