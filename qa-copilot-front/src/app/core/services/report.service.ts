import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly apiUrl = `${environment.apiUrl}/api/reports`;

  constructor(private http: HttpClient) {}

  generateComparison(data: any, file?: File): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/comparison`, this.buildForm(data, file)).pipe(
      map(r => r.data || r)
    );
  }

  generateCompletion(data: any, file?: File): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/completion`, this.buildForm(data, file)).pipe(
      map(r => r.data || r)
    );
  }

  generateInnovation(data: any, file?: File): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/innovation`, this.buildForm(data, file)).pipe(
      map(r => r.data || r)
    );
  }

 private buildForm(data: any, file?: File): FormData {
  const form = new FormData();
  Object.entries(data).forEach(([key, value]) => {
    if (value !== null && value !== undefined) {
      if (key === 'jiraBugs') {
        // Siempre serializar como JSON string
        form.append(key, Array.isArray(value) ? JSON.stringify(value) : String(value));
      } else if (Array.isArray(value)) {
        form.append(key, JSON.stringify(value));
      } else if (typeof value === 'object') {
        form.append(key, JSON.stringify(value));
      } else {
        form.append(key, String(value));
      }
    }
  });
  if (file) form.append('document', file);
  return form;
}
}