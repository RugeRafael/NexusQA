import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardMetrics } from '../models/metrics.model';

@Injectable({ providedIn: 'root' })
export class MetricsService {
  private readonly apiUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  getDashboard(): Observable<DashboardMetrics> {
    return this.http.get<any>(`${this.apiUrl}/metrics/dashboard`).pipe(
      map(response => response.data || response)
    );
  }

  getUsersActivity(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/analytics/users`).pipe(
      map(response => response.data || response)
    );
  }
}