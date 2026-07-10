import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TestplanService {
  private readonly apiUrl = environment.apiUrl + '/api/testplan';

  constructor(private http: HttpClient) {}

  analyzePlan(file: File, projectName?: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    if (projectName) formData.append('project_name', projectName);
    return this.http.post<any>(this.apiUrl + '/analyze-file', formData)
      .pipe(map(r => r.data || r));
  }

  analyzePlanText(content: string, projectName?: string): Observable<any> {
    return this.http.post<any>(this.apiUrl + '/analyze', {
      plan_content: content,
      project_name: projectName || ''
    }).pipe(map(r => r.data || r));
  }

  getHistory(): Observable<any> {
    return this.http.get<any>(this.apiUrl + '/history')
      .pipe(map(r => r.data || r));
  }
}
