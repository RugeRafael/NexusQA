import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TrainingService {
  private readonly apiUrl = `${environment.apiUrl}/api/training`;

  constructor(private http: HttpClient) {}

  upload(file: File, category: string, description?: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('category', category);
    if (description) formData.append('description', description);
    return this.http.post<any>(`${this.apiUrl}/upload`, formData).pipe(
      map(r => r.data || r)
    );
  }

  getAll(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl).pipe(
      map(r => r.data || r)
    );
  }

  setActive(id: string, isActive: boolean): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/active`, { isActive });
  }
}